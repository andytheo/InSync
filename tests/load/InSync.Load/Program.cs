using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

if(args.Length<2){Console.Error.WriteLine("Usage: dotnet run -- <https-api-url> <room-count>");return 2;}
var baseUrl=args[0].TrimEnd('/');if(!Uri.TryCreate(baseUrl,UriKind.Absolute,out var baseUri)||baseUri.Scheme!="https"){Console.Error.WriteLine("Use a deployed HTTPS API URL.");return 2;}
if(!int.TryParse(args[1],out var roomCount)||roomCount<1||roomCount>200){Console.Error.WriteLine("room-count must be 1..200.");return 2;}

using var http=new HttpClient{BaseAddress=baseUri};
var failures=0;
await Parallel.ForEachAsync(Enumerable.Range(0,roomCount),new ParallelOptions{MaxDegreeOfParallelism=20},async(_,ct)=>{
 try{
  var created=await (await http.PostAsJsonAsync("/api/rooms",new{name="Load"},ct)).Content.ReadFromJsonAsync<RoomDto>(cancellationToken:ct);
  if(created is null)throw new Exception("Room creation returned no room.");
  await using var a=Build(baseUri);await using var b=Build(baseUri);
  await a.StartAsync(ct);await b.StartAsync(ct);
  await a.InvokeAsync("JoinRoom",created.code,"A",ct);await b.InvokeAsync("JoinRoom",created.code,"B",ct);
  await a.InvokeAsync("SelectMedia",created.code,new ContentSelection("Load video","youtube","https://youtu.be/dQw4w9WgXcQ"),ct);
  await a.InvokeAsync("PlaybackChanged",created.code,10d,true,ct);
  await b.InvokeAsync("PlaybackChanged",created.code,10d,false,ct);
 }catch(Exception e){Interlocked.Increment(ref failures);Console.Error.WriteLine(e.Message);}
});
Console.WriteLine($"Completed {roomCount} paired rooms ({roomCount*2} SignalR clients); failures={failures}.");
return failures==0?0:1;

static HubConnection Build(Uri baseUri)=>new HubConnectionBuilder().WithUrl(new Uri(baseUri,"/hubs/rooms")).WithAutomaticReconnect().Build();
record RoomDto(string code);
record ContentSelection(string title,string provider,string? url);
