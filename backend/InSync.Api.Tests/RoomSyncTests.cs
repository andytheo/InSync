using System.Net.Http.Json; using Microsoft.Data.Sqlite; using Microsoft.EntityFrameworkCore; using Microsoft.Extensions.DependencyInjection; using Microsoft.AspNetCore.Mvc.Testing; using Microsoft.AspNetCore.SignalR.Client; using Microsoft.AspNetCore.SignalR; using Xunit;
public class RoomSyncTests : IClassFixture<WebApplicationFactory<Program>> {
 readonly WebApplicationFactory<Program> factory; public RoomSyncTests(WebApplicationFactory<Program> f)=>factory=f;
 [Fact] public async Task Two_clients_share_authoritative_play_pause_seek_state(){
  var http=factory.CreateClient(); var created=await (await http.PostAsJsonAsync("/api/rooms",new {name="A"})).Content.ReadFromJsonAsync<RoomDto>(); Assert.NotNull(created);
  var a=Connect(); var b=Connect(); var aStates=new List<PlaybackDto>(); var bStates=new List<PlaybackDto>(); a.On<PlaybackDto>("PlaybackChanged",x=>aStates.Add(x)); b.On<PlaybackDto>("PlaybackChanged",x=>bStates.Add(x));
  await a.StartAsync(); await b.StartAsync(); await a.InvokeAsync("JoinRoom",created!.code,"A"); await b.InvokeAsync("JoinRoom",created.code,"B");
  await a.InvokeAsync("PlaybackChanged",created.code,10d,true); await Wait(()=>bStates.Count>=1); Assert.True(bStates[^1].playing);
  await b.InvokeAsync("PlaybackChanged",created.code,10d,false); await Wait(()=>aStates.Count>=2); Assert.False(aStates[^1].playing);
  await a.InvokeAsync("PlaybackChanged",created.code,42d,false); await Wait(()=>bStates.Count>=3); Assert.Equal(42d,bStates[^1].position); Assert.Equal(3,bStates[^1].sequence);
  await a.DisposeAsync(); await b.DisposeAsync();
 }

 [Fact] public async Task Disconnect_marks_participant_offline_and_reconnect_reuses_identity(){
  var http=factory.CreateClient();var created=await (await http.PostAsJsonAsync("/api/rooms",new {name="Host"})).Content.ReadFromJsonAsync<RoomDto>();Assert.NotNull(created);
  var a=Connect();await a.StartAsync();await a.InvokeAsync("JoinRoom",created!.code,"Guest");await a.DisposeAsync();await Task.Delay(50);
  var afterLeave=await http.GetFromJsonAsync<RoomStateDto>($"/api/rooms/{created.code}");var guest=Assert.Single(afterLeave!.participants,x=>x.name=="Guest");Assert.False(guest.online);
  var id=guest.id;var b=Connect();await b.StartAsync();await b.InvokeAsync("JoinRoom",created.code,"Guest");await Task.Delay(50);
  var afterReturn=await http.GetFromJsonAsync<RoomStateDto>($"/api/rooms/{created.code}");var returned=Assert.Single(afterReturn!.participants,x=>x.name=="Guest");Assert.True(returned.online);Assert.Equal(id,returned.id);
  await b.DisposeAsync();
 }
 [Fact] public async Task Non_member_cannot_change_room_state(){
  var http=factory.CreateClient();var created=await (await http.PostAsJsonAsync("/api/rooms",new {name="Host"})).Content.ReadFromJsonAsync<RoomDto>();Assert.NotNull(created);
  var outsider=Connect();await outsider.StartAsync();await Assert.ThrowsAsync<HubException>(()=>outsider.InvokeAsync("PlaybackChanged",created!.code,99d,true));await outsider.DisposeAsync();
 }
 [Fact] public async Task Media_selection_is_shared_and_persisted(){var http=factory.CreateClient();var created=await (await http.PostAsJsonAsync("/api/rooms",new {name="Host"})).Content.ReadFromJsonAsync<RoomDto>();Assert.NotNull(created);var a=Connect();var b=Connect();ContentSelection? received=null;b.On<ContentSelection>("MediaSelected",x=>received=x);await a.StartAsync();await b.StartAsync();await a.InvokeAsync("JoinRoom",created!.code,"A");await b.InvokeAsync("JoinRoom",created.code,"B");await a.InvokeAsync("SelectMedia",created.code,new ContentSelection("Test video","youtube","https://youtu.be/dQw4w9WgXcQ"));await Wait(()=>received is not null);Assert.Equal("youtube",received!.Provider);Assert.Equal("Test video",received.Title);await a.DisposeAsync();await b.DisposeAsync();}
 [Fact] public async Task Reactions_are_shared_with_the_other_participant(){
  var http=factory.CreateClient();var created=await (await http.PostAsJsonAsync("/api/rooms",new {name="Host"})).Content.ReadFromJsonAsync<RoomDto>();Assert.NotNull(created);
  var a=Connect();var b=Connect();string? reaction=null;b.On<string>("ReactionSet",x=>reaction=x);
  await a.StartAsync();await b.StartAsync();await a.InvokeAsync("JoinRoom",created!.code,"A");await b.InvokeAsync("JoinRoom",created.code,"B");await a.InvokeAsync("React",created.code,"👏");await Wait(()=>reaction=="👏");await a.DisposeAsync();await b.DisposeAsync();
 }
 [Fact] public async Task Playback_sequence_ignores_insignificant_duplicate_playing_update(){var http=factory.CreateClient();var created=await (await http.PostAsJsonAsync("/api/rooms",new {name="Host"})).Content.ReadFromJsonAsync<RoomDto>();Assert.NotNull(created);var a=Connect();await a.StartAsync();await a.InvokeAsync("JoinRoom",created!.code,"A");await a.InvokeAsync("PlaybackChanged",created.code,10d,true);await a.InvokeAsync("PlaybackChanged",created.code,10.1d,true);var state=await http.GetFromJsonAsync<PlaybackRoomDto>($"/api/rooms/{created.code}");Assert.Equal(1,state!.playback.sequence);await a.DisposeAsync();}
 [Fact] public async Task Invalid_creator_and_join_names_are_rejected(){
  var http=factory.CreateClient();var bad=await http.PostAsJsonAsync("/api/rooms",new {name=""});Assert.Equal(System.Net.HttpStatusCode.BadRequest,bad.StatusCode);var created=await (await http.PostAsJsonAsync("/api/rooms",new {name="Host"})).Content.ReadFromJsonAsync<RoomDto>();var a=Connect();await a.StartAsync();await Assert.ThrowsAsync<HubException>(()=>a.InvokeAsync("JoinRoom",created!.code,""));await a.DisposeAsync();
 }
 [Fact] public async Task Extreme_playback_positions_are_bounded(){
  var http=factory.CreateClient();var created=await (await http.PostAsJsonAsync("/api/rooms",new {name="Host"})).Content.ReadFromJsonAsync<RoomDto>();var a=Connect();await a.StartAsync();await a.InvokeAsync("JoinRoom",created!.code,"A");await a.InvokeAsync("PlaybackChanged",created.code,999999d,false);var state=await http.GetFromJsonAsync<PlaybackRoomDto>($"/api/rooms/{created.code}");Assert.Equal(86400d,state!.playback.position);await a.DisposeAsync();
 }
 [Fact] public async Task Health_and_readiness_endpoints_are_available(){var client=factory.CreateClient();var health=await client.GetAsync("/health");var ready=await client.GetAsync("/ready");Assert.True(health.IsSuccessStatusCode);Assert.True(ready.IsSuccessStatusCode);}
 [Fact] public async Task New_room_codes_are_cryptographically_sized_and_url_safe(){var room=await (await factory.CreateClient().PostAsJsonAsync("/api/rooms",new{name="Host"})).Content.ReadFromJsonAsync<RoomDto>();Assert.NotNull(room);Assert.Matches("^[A-HJ-NP-Z2-9]{8}$",room!.code);}
 [Fact] public async Task Ended_room_is_removed_by_cleanup(){
  var http=factory.CreateClient();var created=await (await http.PostAsJsonAsync("/api/rooms",new {name="Host"})).Content.ReadFromJsonAsync<RoomDto>();Assert.NotNull(created);
  var hub=Connect();await hub.StartAsync();await hub.InvokeAsync("JoinRoom",created!.code,"Host");await hub.InvokeAsync("EndRoom",created.code);await hub.DisposeAsync();
  var store=factory.Services.GetRequiredService<RoomStore>();Assert.Equal(1,store.CleanupExpired(TimeSpan.FromHours(6)));Assert.Null(store.Get(created.code));
 }
 [Fact] public void Room_store_restores_persisted_room_state(){
  using var connection=new SqliteConnection("Data Source=:memory:");connection.Open();
  var options=new DbContextOptionsBuilder<InSyncDbContext>().UseSqlite(connection).Options;
  using(var db=new InSyncDbContext(options))db.Database.EnsureCreated();
  var factory=new TestDbFactory(options);var first=new RoomStore(factory);var room=first.Create("Host");var guest=first.Join(room.Code,"connection-1","Guest");room.Content="Movie night";room.Playback=new Playback(123,true,7,DateTimeOffset.UtcNow);first.Save(room);
  var restored=new RoomStore(factory);var loaded=restored.Get(room.Code);Assert.NotNull(loaded);Assert.Equal("Movie night",loaded!.Content);Assert.Equal(123,loaded.Playback.Position);Assert.Equal(7,loaded.Playback.Sequence);Assert.Contains(loaded.Participants,x=>x.Id==guest.Id&&x.Name=="Guest");Assert.All(loaded.Participants,x=>Assert.False(x.Online));
 }
 sealed class TestDbFactory(DbContextOptions<InSyncDbContext> options):IDbContextFactory<InSyncDbContext>{public InSyncDbContext CreateDbContext()=>new(options);}
 HubConnection Connect()=>new HubConnectionBuilder().WithUrl(new Uri(factory.Server.BaseAddress,"/hubs/rooms"),o=>o.HttpMessageHandlerFactory=_=>factory.Server.CreateHandler()).Build();
 static async Task Wait(Func<bool> ok){for(var i=0;i<50&&!ok();i++)await Task.Delay(20);Assert.True(ok());}
 record RoomDto(string code,PlaybackDto playback); record PlaybackRoomDto(string code,PlaybackDto playback); record RoomStateDto(string code,List<ParticipantDto> participants); record ParticipantDto(Guid id,string name,bool ready,bool online); record PlaybackDto(double position,bool playing,long sequence,DateTimeOffset updatedAt);
}