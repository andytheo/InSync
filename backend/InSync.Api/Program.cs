using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

var b=WebApplication.CreateBuilder(args);
b.Services.AddSignalR();
var dbConnection=b.Configuration.GetConnectionString("InSync");
if(!string.IsNullOrWhiteSpace(dbConnection)) b.Services.AddDbContextFactory<InSyncDbContext>(o=>o.UseNpgsql(dbConnection));
b.Services.AddSingleton<RoomStore>(sp=>new RoomStore(sp.GetService<IDbContextFactory<InSyncDbContext>>()));
b.Services.AddHostedService<RoomCleanupService>();
b.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_=>true).AllowCredentials()));
var app=b.Build();
app.UseCors();
if(!string.IsNullOrWhiteSpace(dbConnection)){using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<InSyncDbContext>();await db.Database.MigrateAsync();}

app.MapGet("/health",()=>Results.Ok(new {status="ok",time=DateTimeOffset.UtcNow}));
app.MapPost("/api/rooms",(CreateRoom x,RoomStore s)=>{var name=(x.Name??"").Trim();return name.Length is <1 or >80?Results.BadRequest(new{error="Enter a name up to 80 characters."}):Results.Ok(s.Create(name));});
app.MapGet("/api/rooms/{code}",(string code,RoomStore s)=>s.Get(code) is {} r?Results.Ok(r):Results.NotFound());
app.MapHub<RoomHub>("/hubs/rooms");
app.Run();

public partial class Program { }
record CreateRoom(string? Name);
public record Participant(Guid Id,string Name,bool Ready=false,bool Online=true);
public record Playback(double Position,bool Playing,long Sequence,DateTimeOffset UpdatedAt);
public record ContentSelection(string Title,string Provider,string? Url);
public record ScheduledStart(DateTimeOffset StartsAt,double Position,long Sequence);

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
 public RoomStore(IDbContextFactory<InSyncDbContext>? dbFactory=null){this.dbFactory=dbFactory;LoadPersisted();}
 public Room Create(string name){string c;do c=Random.Shared.Next(100000,999999).ToString();while(rooms.ContainsKey(c));var r=new Room{Code=c};rooms[c]=r;Persist(r);return r;}
 public Room? Get(string c)=>rooms.GetValueOrDefault(c);
 public int CleanupExpired(TimeSpan idleFor){var cutoff=DateTimeOffset.UtcNow-idleFor;var removed=0;foreach(var x in rooms){var r=x.Value;bool expire;lock(r.Gate)expire=r.Ended||(!r.Participants.Any(p=>p.Online)&&r.LastActivityAt<cutoff);if(expire&&rooms.TryRemove(x.Key,out _)){DeletePersisted(x.Key);removed++;}}return removed;}
 public Participant Join(string code,string connectionId,string name){
  var r=Get(code)??throw new HubException("Room not found");Participant p;
  lock(r.Gate){var i=r.Participants.FindIndex(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)&&!x.Online);if(i>=0){p=r.Participants[i] with{Online=true};r.Participants[i]=p;}else{p=new(Guid.NewGuid(),name);r.Participants.Add(p);}r.LastActivityAt=DateTimeOffset.UtcNow;}
  connections[connectionId]=(code,p.Id);Persist(r);return p;
 }
 public (string Code,Participant Participant)? Leave(string connectionId){
  if(!connections.TryRemove(connectionId,out var link)||Get(link.Code) is not {} r)return null;
  Participant p;lock(r.Gate){var i=r.Participants.FindIndex(x=>x.Id==link.ParticipantId);if(i<0)return null;p=r.Participants[i] with{Online=false,Ready=false};r.Participants[i]=p;r.LastActivityAt=DateTimeOffset.UtcNow;}Persist(r);return(link.Code,p);
 }
 public void Save(Room r)=>Persist(r);
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
  db.SaveChanges();
 }
 void DeletePersisted(string code){if(dbFactory is null)return;using var db=dbFactory.CreateDbContext();var e=db.Rooms.SingleOrDefault(x=>x.Code==code);if(e is null)return;db.Rooms.Remove(e);db.SaveChanges();}
}

sealed class RoomHub(RoomStore store):Hub {
 Room Get(string c)=>store.Get(c)??throw new HubException("Room not found");
 public async Task JoinRoom(string c,string name){name=(name??"").Trim();if(name.Length<1||name.Length>80)throw new HubException("Enter a name up to 80 characters.");var p=store.Join(c,Context.ConnectionId,name);await Groups.AddToGroupAsync(Context.ConnectionId,c);await Clients.Group(c).SendAsync("ParticipantJoined",p);}
 public async Task SetReady(string c,bool ready){var (r,p)=store.RequireMember(c,Context.ConnectionId);lock(r.Gate){var i=r.Participants.FindIndex(x=>x.Id==p.Id);r.Participants[i]=r.Participants[i] with{Ready=ready};r.LastActivityAt=DateTimeOffset.UtcNow;}store.Save(r);await Clients.Group(c).SendAsync("ParticipantReady",p.Id,ready);}
 public async Task SelectContent(string c,string content){await SelectMedia(c,new ContentSelection(content,"other",null));}
 public async Task SelectMedia(string c,ContentSelection selection){var (r,_)=store.RequireMember(c,Context.ConnectionId);var clean=new ContentSelection((selection.Title??"").Trim(),(selection.Provider??"other").Trim().ToLowerInvariant(),string.IsNullOrWhiteSpace(selection.Url)?null:selection.Url.Trim());if(string.IsNullOrWhiteSpace(clean.Title))throw new HubException("Choose something to watch.");if(clean.Title.Length>200||clean.Provider.Length>40||(clean.Url?.Length??0)>2048)throw new HubException("Media details are too long.");if(clean.Url is not null&&(!Uri.TryCreate(clean.Url,UriKind.Absolute,out var parsed)||!(parsed.Scheme==Uri.UriSchemeHttp||parsed.Scheme==Uri.UriSchemeHttps)))throw new HubException("Use a valid web link.");if(clean.Provider=="youtube"&&(clean.Url is null||!Uri.TryCreate(clean.Url,UriKind.Absolute,out var mediaUri)||!(mediaUri.Host.Equals("youtube.com",StringComparison.OrdinalIgnoreCase)||mediaUri.Host.EndsWith(".youtube.com",StringComparison.OrdinalIgnoreCase)||mediaUri.Host.Equals("youtu.be",StringComparison.OrdinalIgnoreCase))))throw new HubException("Use a valid YouTube link.");lock(r.Gate){r.Selection=clean;r.Content=clean.Title;r.LastActivityAt=DateTimeOffset.UtcNow;}store.Save(r);await Clients.Group(c).SendAsync("MediaSelected",clean);await Clients.Group(c).SendAsync("ContentSelected",clean.Title);}
 public async Task StartTogether(string c,double pos){if(!double.IsFinite(pos))throw new HubException("Invalid playback position.");var (r,_)=store.RequireMember(c,Context.ConnectionId);bool allReady;lock(r.Gate)allReady=r.Participants.Count(x=>x.Online)>=2&&r.Participants.Where(x=>x.Online).All(x=>x.Ready);if(!allReady)throw new HubException("Everyone in the room must be ready.");if(r.Selection is null)throw new HubException("Choose something to watch first.");if(r.Selection.Provider!="youtube"||string.IsNullOrWhiteSpace(r.Selection.Url))throw new HubException("Start Together currently requires a YouTube video.");ScheduledStart start;lock(r.Gate){var safePos=Math.Clamp(pos,0,86400);var startsAt=DateTimeOffset.UtcNow.AddSeconds(3);var next=new Playback(safePos,true,r.Playback.Sequence+1,startsAt);r.Playback=next;r.LastActivityAt=DateTimeOffset.UtcNow;start=new ScheduledStart(startsAt,safePos,next.Sequence);}store.Save(r);await Clients.Group(c).SendAsync("StartTogether",start);await Clients.Group(c).SendAsync("PlaybackChanged",r.Playback);}
 public async Task PlaybackChanged(string c,double pos,bool playing){var (r,_)=store.RequireMember(c,Context.ConnectionId);Playback next;lock(r.Gate){if(!double.IsFinite(pos))throw new HubException("Invalid playback position.");var safePos=Math.Clamp(pos,0,86400);var expected=r.Playback.Playing?r.Playback.Position+Math.Max(0,(DateTimeOffset.UtcNow-r.Playback.UpdatedAt).TotalSeconds):r.Playback.Position;if(r.Playback.Playing&&playing&&Math.Abs(safePos-expected)<0.75)return;next=new(safePos,playing,r.Playback.Sequence+1,DateTimeOffset.UtcNow);r.Playback=next;r.LastActivityAt=DateTimeOffset.UtcNow;}store.Save(r);await Clients.Group(c).SendAsync("PlaybackChanged",next);}
 public Task React(string c,string emoji){if(string.IsNullOrWhiteSpace(emoji)||emoji.Length>8)throw new HubException("Invalid reaction.");var (r,_)=store.RequireMember(c,Context.ConnectionId);lock(r.Gate)r.LastActivityAt=DateTimeOffset.UtcNow;store.Save(r);return Clients.OthersInGroup(c).SendAsync("ReactionSet",emoji);}
 public async Task EndRoom(string c){var (r,_)=store.RequireMember(c,Context.ConnectionId);lock(r.Gate){r.Ended=true;r.LastActivityAt=DateTimeOffset.UtcNow;}store.Save(r);await Clients.Group(c).SendAsync("RoomEnded");}
 public override async Task OnDisconnectedAsync(Exception? exception){var left=store.Leave(Context.ConnectionId);if(left is {} x)await Clients.Group(x.Code).SendAsync("ParticipantLeft",x.Participant);await base.OnDisconnectedAsync(exception);}
}

sealed class RoomCleanupService(RoomStore store,ILogger<RoomCleanupService> logger):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){using var timer=new PeriodicTimer(TimeSpan.FromMinutes(5));while(await timer.WaitForNextTickAsync(stoppingToken)){var n=store.CleanupExpired(TimeSpan.FromHours(6));if(n>0)logger.LogInformation("Expired {Count} inactive InSync rooms",n);}}
}
