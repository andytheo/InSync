using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

var b=WebApplication.CreateBuilder(args);
b.Services.AddSignalR();
b.Services.AddSingleton<RoomStore>();
b.Services.AddHostedService<RoomCleanupService>();
b.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_=>true).AllowCredentials()));
var app=b.Build();
app.UseCors();

app.MapGet("/health",()=>Results.Ok(new {status="ok",time=DateTimeOffset.UtcNow}));
app.MapPost("/api/rooms",(CreateRoom x,RoomStore s)=>Results.Ok(s.Create(x.Name)));
app.MapGet("/api/rooms/{code}",(string code,RoomStore s)=>s.Get(code) is {} r?Results.Ok(r):Results.NotFound());
app.MapHub<RoomHub>("/hubs/rooms");
app.Run();

public partial class Program { }
record CreateRoom(string Name);
record Participant(Guid Id,string Name,bool Ready=false,bool Online=true);
record Playback(double Position,bool Playing,long Sequence,DateTimeOffset UpdatedAt);

sealed class Room {
 public required string Code{get;init;}
 public List<Participant> Participants{get;}=[];
 public string? Content{get;set;}
 public Playback Playback{get;set;}=new(0,false,0,DateTimeOffset.UtcNow);
 public bool Ended{get;set;}
 public object Gate{get;}=new();
 public DateTimeOffset CreatedAt{get;}=DateTimeOffset.UtcNow;
 public DateTimeOffset LastActivityAt{get;set;}=DateTimeOffset.UtcNow;
}

sealed class RoomStore {
 readonly ConcurrentDictionary<string,Room> rooms=new();
 readonly ConcurrentDictionary<string,(string Code,Guid ParticipantId)> connections=new();
 public Room Create(string name){string c;do c=Random.Shared.Next(100000,999999).ToString();while(rooms.ContainsKey(c));var r=new Room{Code=c};rooms[c]=r;return r;}
 public Room? Get(string c)=>rooms.GetValueOrDefault(c);
 public int CleanupExpired(TimeSpan idleFor){var cutoff=DateTimeOffset.UtcNow-idleFor;var removed=0;foreach(var x in rooms){var r=x.Value;bool expire;lock(r.Gate)expire=r.Ended||(!r.Participants.Any(p=>p.Online)&&r.LastActivityAt<cutoff);if(expire&&rooms.TryRemove(x.Key,out _))removed++;}return removed;}
 public Participant Join(string code,string connectionId,string name){
  var r=Get(code)??throw new HubException("Room not found");
  Participant p;
  lock(r.Gate){
   var i=r.Participants.FindIndex(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)&&!x.Online);
   if(i>=0){p=r.Participants[i] with{Online=true};r.Participants[i]=p;}
   else{p=new(Guid.NewGuid(),name);r.Participants.Add(p);}
  }
  r.LastActivityAt=DateTimeOffset.UtcNow;connections[connectionId]=(code,p.Id);return p;
 }
 public (string Code,Participant Participant)? Leave(string connectionId){
  if(!connections.TryRemove(connectionId,out var link)||Get(link.Code) is not {} r)return null;
  lock(r.Gate){var i=r.Participants.FindIndex(x=>x.Id==link.ParticipantId);if(i<0)return null;var p=r.Participants[i] with{Online=false,Ready=false};r.Participants[i]=p;r.LastActivityAt=DateTimeOffset.UtcNow;return(link.Code,p);}
 }
}

sealed class RoomHub(RoomStore store):Hub {
 Room Get(string c)=>store.Get(c)??throw new HubException("Room not found");
 public async Task JoinRoom(string c,string name){var p=store.Join(c,Context.ConnectionId,name);await Groups.AddToGroupAsync(Context.ConnectionId,c);await Clients.Group(c).SendAsync("ParticipantJoined",p);}
 public async Task SetReady(string c,string name,bool ready){var r=Get(c);lock(r.Gate){var i=r.Participants.FindLastIndex(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)&&x.Online);if(i<0)return;var id=r.Participants[i].Id;r.Participants[i]=r.Participants[i] with{Ready=ready};Clients.Group(c).SendAsync("ParticipantReady",id,ready).GetAwaiter().GetResult();}}
 public async Task SelectContent(string c,string content){var r=Get(c);lock(r.Gate){r.Content=content;r.LastActivityAt=DateTimeOffset.UtcNow;}await Clients.Group(c).SendAsync("ContentSelected",content);}
 public async Task PlaybackChanged(string c,double pos,bool playing){var r=Get(c);Playback next;lock(r.Gate){next=new(Math.Max(0,pos),playing,r.Playback.Sequence+1,DateTimeOffset.UtcNow);r.Playback=next;r.LastActivityAt=DateTimeOffset.UtcNow;}await Clients.Group(c).SendAsync("PlaybackChanged",next);}
 public Task React(string c,string emoji)=>Clients.OthersInGroup(c).SendAsync("ReactionSet",emoji);
 public async Task EndRoom(string c){var r=Get(c);lock(r.Gate){r.Ended=true;r.LastActivityAt=DateTimeOffset.UtcNow;}await Clients.Group(c).SendAsync("RoomEnded");}
 public override async Task OnDisconnectedAsync(Exception? exception){var left=store.Leave(Context.ConnectionId);if(left is {} x)await Clients.Group(x.Code).SendAsync("ParticipantLeft",x.Participant);await base.OnDisconnectedAsync(exception);}
}

sealed class RoomCleanupService(RoomStore store,ILogger<RoomCleanupService> logger):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){using var timer=new PeriodicTimer(TimeSpan.FromMinutes(5));while(await timer.WaitForNextTickAsync(stoppingToken)){var n=store.CleanupExpired(TimeSpan.FromHours(6));if(n>0)logger.LogInformation("Expired {Count} inactive InSync rooms",n);}}
}
