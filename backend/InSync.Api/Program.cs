using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Security.Cryptography;

var b=WebApplication.CreateBuilder(args);
b.Services.AddSignalR(o=>{o.MaximumReceiveMessageSize=32*1024;o.EnableDetailedErrors=b.Environment.IsDevelopment();});
b.Services.AddRateLimiter(o=>{
 o.RejectionStatusCode=StatusCodes.Status429TooManyRequests;
 o.AddFixedWindowLimiter("room-create",x=>{x.PermitLimit=20;x.Window=TimeSpan.FromMinutes(1);x.QueueLimit=0;x.AutoReplenishment=true;});
 o.AddFixedWindowLimiter("room-read",x=>{x.PermitLimit=120;x.Window=TimeSpan.FromMinutes(1);x.QueueLimit=0;x.AutoReplenishment=true;});
 o.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext,string>(ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=300,Window=TimeSpan.FromMinutes(1),QueueLimit=0,AutoReplenishment=true}));
});
var dbConnection=b.Configuration.GetConnectionString("InSync");
var allowedOrigins=(b.Configuration["AllowedOrigins"]??"").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
if(!b.Environment.IsDevelopment()&&string.IsNullOrWhiteSpace(dbConnection))throw new InvalidOperationException("Production requires ConnectionStrings__InSync.");
if(!b.Environment.IsDevelopment()&&allowedOrigins.Length==0)throw new InvalidOperationException("Production requires AllowedOrigins.");
if(!string.IsNullOrWhiteSpace(dbConnection)) b.Services.AddDbContextFactory<InSyncDbContext>(o=>o.UseNpgsql(dbConnection));
b.Services.AddSingleton<RoomStore>(sp=>new RoomStore(sp.GetService<IDbContextFactory<InSyncDbContext>>(),sp.GetRequiredService<ILogger<RoomStore>>()));
b.Services.AddHostedService<RoomCleanupService>();
b.Services.AddCors(o=>o.AddDefaultPolicy(p=>{
 p.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
 if(b.Environment.IsDevelopment())p.SetIsOriginAllowed(_=>true);else p.WithOrigins(allowedOrigins);
}));
var app=b.Build();
if(!app.Environment.IsDevelopment()){app.UseHsts();app.UseHttpsRedirection();}
app.UseCors();
app.UseRateLimiter();
if(!string.IsNullOrWhiteSpace(dbConnection)){using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<InSyncDbContext>();await db.Database.MigrateAsync();}

app.MapGet("/health",()=>Results.Ok(new {status="ok",time=DateTimeOffset.UtcNow}));
app.MapGet("/ready",async (IServiceProvider services)=>{
 if(string.IsNullOrWhiteSpace(dbConnection))return app.Environment.IsDevelopment()?Results.Ok(new{status="ready",database="disabled"}):Results.StatusCode(503);
 try{await using var scope=services.CreateAsyncScope();var factory=scope.ServiceProvider.GetRequiredService<IDbContextFactory<InSyncDbContext>>();await using var db=await factory.CreateDbContextAsync();return await db.Database.CanConnectAsync()?Results.Ok(new{status="ready",database="connected"}):Results.StatusCode(503);}catch{return Results.StatusCode(503);}
});
app.MapPost("/api/rooms",(CreateRoom x,RoomStore s)=>{var name=(x.Name??"").Trim();return name.Length is <1 or >80?Results.BadRequest(new{error="Enter a name up to 80 characters."}):Results.Ok(s.Create(name));}).RequireRateLimiting("room-create");
app.MapGet("/api/rooms/{code}",(string code,RoomStore s)=>s.Get(code) is {} r?Results.Ok(r):Results.NotFound()).RequireRateLimiting("room-read");
app.MapHub<RoomHub>("/hubs/rooms");
app.Run();

public partial class Program { }
record CreateRoom(string? Name);
public record Participant(Guid Id,string Name,bool Ready=false,bool Online=true);
public record Playback(double Position,bool Playing,long Sequence,DateTimeOffset UpdatedAt);
public record ContentSelection(string Title,string Provider,string? Url);

public sealed class Room {
 public required string Code{get;init;}
 public List<Participant> Participants{get;}=[];
 public string? Content{get;set;}
 public ContentSelection? Selection{get;set;}
 public Playback Playback{get;set;}=new(0,false,0,DateTimeOffset.UtcNow);
 public bool Ended{get;set;}
 public object Gate{get;}=new();
 public DateTimeOffset CreatedAt{get;}=DateTimeOffset.UtcNow;
 public DateTimeOffset LastActivityAt{get;set;}=DateTimeOffset.UtcNow;
}

public sealed class RoomStore {
 readonly ConcurrentDictionary<string,Room> rooms=new();
 readonly ConcurrentDictionary<string,(string Code,Guid ParticipantId)> connections=new();
 readonly IDbContextFactory<InSyncDbContext>? dbFactory;
 readonly ILogger<RoomStore>? logger;
 readonly ConcurrentDictionary<string,DateTimeOffset> playbackCheckpoints=new();
 public RoomStore(IDbContextFactory<InSyncDbContext>? dbFactory=null,ILogger<RoomStore>? logger=null){this.dbFactory=dbFactory;this.logger=logger;LoadPersisted();}
 public Room Create(string name){const string alphabet="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";string c;do c=RandomNumberGenerator.GetString(alphabet,8);while(rooms.ContainsKey(c));var r=new Room{Code=c};rooms[c]=r;Persist(r);return r;}
 public Room? Get(string c)=>rooms.GetValueOrDefault(c);
 public int CleanupExpired(TimeSpan idleFor){var cutoff=DateTimeOffset.UtcNow-idleFor;var removed=0;foreach(var x in rooms){var r=x.Value;bool expire;lock(r.Gate)expire=r.Ended||(!r.Participants.Any(p=>p.Online)&&r.LastActivityAt<cutoff);if(expire&&rooms.TryRemove(x.Key,out _)){DeletePersisted(x.Key);removed++;}}return removed;}
 public Participant Join(string code,string connectionId,string name){
  var r=Get(code)??throw new HubException("Room not found");Participant p;
  lock(r.Gate){if(r.Ended)throw new HubException("Room has ended.");if(r.Participants.Count(x=>x.Online)>=2&&!r.Participants.Any(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)&&!x.Online))throw new HubException("This room already has two people.");var i=r.Participants.FindIndex(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)&&!x.Online);if(i>=0){p=r.Participants[i] with{Online=true};r.Participants[i]=p;}else{p=new(Guid.NewGuid(),name);r.Participants.Add(p);}r.LastActivityAt=DateTimeOffset.UtcNow;}
  connections[connectionId]=(code,p.Id);Persist(r);return p;
 }
 public (string Code,Participant Participant)? Leave(string connectionId){
  if(!connections.TryRemove(connectionId,out var link)||Get(link.Code) is not {} r)return null;
  Participant p;lock(r.Gate){var i=r.Participants.FindIndex(x=>x.Id==link.ParticipantId);if(i<0)return null;p=r.Participants[i] with{Online=false,Ready=false};r.Participants[i]=p;r.LastActivityAt=DateTimeOffset.UtcNow;}Persist(r);return(link.Code,p);
 }
 public void Save(Room r)=>Persist(r);
 public void SavePlaybackCheckpoint(Room r){var now=DateTimeOffset.UtcNow;if(!playbackCheckpoints.TryGetValue(r.Code,out var last)||now-last>=TimeSpan.FromSeconds(10)||!r.Playback.Playing){playbackCheckpoints[r.Code]=now;Persist(r);}}
 public (Room Room,Participant Participant) RequireMember(string code,string connectionId){if(!connections.TryGetValue(connectionId,out var link)||link.Code!=code)throw new HubException("Join the room before changing it.");var r=Get(code)??throw new HubException("Room not found");var p=r.Participants.SingleOrDefault(x=>x.Id==link.ParticipantId&&x.Online)??throw new HubException("Participant is not connected");return(r,p);}
 void LoadPersisted(){
  if(dbFactory is null)return;using var db=dbFactory.CreateDbContext();foreach(var e in db.Rooms.AsNoTracking().Include(x=>x.Participants).Where(x=>!x.Ended)){
   var r=new Room{Code=e.Code,Content=e.Content,Selection=e.Content is null?null:new ContentSelection(e.Content,e.Provider??"other",e.MediaUrl),Playback=new(e.PlaybackPosition,e.PlaybackPlaying,e.PlaybackSequence,e.PlaybackUpdatedAt),Ended=e.Ended,LastActivityAt=e.LastActivityAt};
   r.Participants.AddRange(e.Participants.Select(p=>new Participant(p.Id,p.Name,false,false)));rooms[r.Code]=r;
  }
 }
 void Persist(Room r){
  if(dbFactory is null)return;using var db=dbFactory.CreateDbContext();var e=db.Rooms.Include(x=>x.Participants).SingleOrDefault(x=>x.Code==r.Code);
  if(e is null){e=new RoomEntity{Code=r.Code};db.Rooms.Add(e);}
  e.Content=r.Content;e.Provider=r.Selection?.Provider;e.MediaUrl=r.Selection?.Url;e.PlaybackPosition=r.Playback.Position;e.PlaybackPlaying=r.Playback.Playing;e.PlaybackSequence=r.Playback.Sequence;e.PlaybackUpdatedAt=r.Playback.UpdatedAt;e.Ended=r.Ended;e.LastActivityAt=r.LastActivityAt;
  var ids=r.Participants.Select(x=>x.Id).ToHashSet();db.Participants.RemoveRange(e.Participants.Where(x=>!ids.Contains(x.Id)));
  foreach(var p in r.Participants){var pe=e.Participants.SingleOrDefault(x=>x.Id==p.Id);if(pe is null){pe=new ParticipantEntity{Id=p.Id,RoomCode=r.Code,Name=p.Name};e.Participants.Add(pe);}pe.Name=p.Name;pe.Ready=p.Ready;pe.Online=p.Online;}
  try{db.SaveChanges();}catch(Exception ex){logger?.LogError(ex,"Failed to persist room {RoomCode}",r.Code);throw;}
 }
 void DeletePersisted(string code){if(dbFactory is null)return;using var db=dbFactory.CreateDbContext();var e=db.Rooms.SingleOrDefault(x=>x.Code==code);if(e is null)return;db.Rooms.Remove(e);db.SaveChanges();}
}

sealed class RoomHub(RoomStore store):Hub {
 Room Get(string c)=>store.Get(c)??throw new HubException("Room not found");
 public async Task JoinRoom(string c,string name){name=(name??"").Trim();if(name.Length<1||name.Length>80)throw new HubException("Enter a name up to 80 characters.");var p=store.Join(c,Context.ConnectionId,name);await Groups.AddToGroupAsync(Context.ConnectionId,c);await Clients.Group(c).SendAsync("ParticipantJoined",p);}
 public async Task SelectMedia(string c,ContentSelection selection){var (r,_)=store.RequireMember(c,Context.ConnectionId);var clean=new ContentSelection((selection.Title??"").Trim(),(selection.Provider??"other").Trim().ToLowerInvariant(),string.IsNullOrWhiteSpace(selection.Url)?null:selection.Url.Trim());if(string.IsNullOrWhiteSpace(clean.Title))throw new HubException("Choose something to watch.");if(clean.Title.Length>200||clean.Provider.Length>40||(clean.Url?.Length??0)>2048)throw new HubException("Media details are too long.");if(clean.Url is not null&&(!Uri.TryCreate(clean.Url,UriKind.Absolute,out var parsed)||!(parsed.Scheme==Uri.UriSchemeHttp||parsed.Scheme==Uri.UriSchemeHttps)))throw new HubException("Use a valid web link.");if(clean.Provider=="youtube"&&(clean.Url is null||!Uri.TryCreate(clean.Url,UriKind.Absolute,out var mediaUri)||!(mediaUri.Host.Equals("youtube.com",StringComparison.OrdinalIgnoreCase)||mediaUri.Host.EndsWith(".youtube.com",StringComparison.OrdinalIgnoreCase)||mediaUri.Host.Equals("youtu.be",StringComparison.OrdinalIgnoreCase))))throw new HubException("Use a valid YouTube link.");lock(r.Gate){r.Selection=clean;r.Content=clean.Title;r.LastActivityAt=DateTimeOffset.UtcNow;}store.Save(r);await Clients.Group(c).SendAsync("MediaSelected",clean);}
 public async Task PlaybackChanged(string c,double pos,bool playing){var (r,_)=store.RequireMember(c,Context.ConnectionId);if(r.Selection is null)throw new HubException("Choose a video before controlling playback.");Playback next;lock(r.Gate){if(!double.IsFinite(pos))throw new HubException("Invalid playback position.");var safePos=Math.Clamp(pos,0,86400);var expected=r.Playback.Playing?r.Playback.Position+Math.Max(0,(DateTimeOffset.UtcNow-r.Playback.UpdatedAt).TotalSeconds):r.Playback.Position;if(r.Playback.Playing&&playing&&Math.Abs(safePos-expected)<0.75)return;next=new(safePos,playing,r.Playback.Sequence+1,DateTimeOffset.UtcNow);r.Playback=next;r.LastActivityAt=DateTimeOffset.UtcNow;}store.SavePlaybackCheckpoint(r);await Clients.Group(c).SendAsync("PlaybackChanged",next);}
 public Task React(string c,string emoji){string[] allowed=["❤️","😂","👏","🔥","😮"];if(!allowed.Contains(emoji))throw new HubException("Invalid reaction.");var (r,_)=store.RequireMember(c,Context.ConnectionId);lock(r.Gate)r.LastActivityAt=DateTimeOffset.UtcNow;store.Save(r);return Clients.OthersInGroup(c).SendAsync("ReactionSet",emoji);}
 public async Task EndRoom(string c){var (r,_)=store.RequireMember(c,Context.ConnectionId);lock(r.Gate){r.Ended=true;r.LastActivityAt=DateTimeOffset.UtcNow;}store.Save(r);await Clients.Group(c).SendAsync("RoomEnded");}
 public override async Task OnDisconnectedAsync(Exception? exception){var left=store.Leave(Context.ConnectionId);if(left is {} x)await Clients.Group(x.Code).SendAsync("ParticipantLeft",x.Participant);await base.OnDisconnectedAsync(exception);}
}

sealed class RoomCleanupService(RoomStore store,ILogger<RoomCleanupService> logger):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){using var timer=new PeriodicTimer(TimeSpan.FromMinutes(5));while(await timer.WaitForNextTickAsync(stoppingToken)){var n=store.CleanupExpired(TimeSpan.FromHours(6));if(n>0)logger.LogInformation("Expired {Count} inactive InSync rooms",n);}}
}
