using System.Net.Http.Json; using Microsoft.AspNetCore.Mvc.Testing; using Microsoft.AspNetCore.SignalR.Client; using Xunit;
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
 [Fact] public async Task Health_endpoint_is_available(){var r=await factory.CreateClient().GetAsync("/health");Assert.True(r.IsSuccessStatusCode);}
 HubConnection Connect()=>new HubConnectionBuilder().WithUrl(new Uri(factory.Server.BaseAddress,"/hubs/rooms"),o=>o.HttpMessageHandlerFactory=_=>factory.Server.CreateHandler()).Build();
 static async Task Wait(Func<bool> ok){for(var i=0;i<50&&!ok();i++)await Task.Delay(20);Assert.True(ok());}
 record RoomDto(string code,PlaybackDto playback); record RoomStateDto(string code,List<ParticipantDto> participants); record ParticipantDto(Guid id,string name,bool ready,bool online); record PlaybackDto(double position,bool playing,long sequence,DateTimeOffset updatedAt);
}