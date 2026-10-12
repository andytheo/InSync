// InSync MV3 background adapter: room transport, not Netflix authentication.
// The extension never reads credentials, cookies, DRM data or media bytes.
const API="https://insync-api-tlfk.onrender.com";
const sessions=new Map();
const pause=ms=>new Promise(r=>setTimeout(r,ms));
async function negotiate(){
 const response=await fetch(API+"/hubs/rooms/negotiate?negotiateVersion=1",{method:"POST"});
 if(!response.ok)throw new Error("SignalR negotiation failed ("+response.status+")");
 const data=await response.json();
 if(!data.connectionToken)throw new Error("Missing SignalR connection token");
 return data.connectionToken;
}
function hubFrame(target,args){return JSON.stringify({type:1,target,arguments:args})+"\x1e"}
async function poll(session){
 while(sessions.get(session.tabId)===session){
  try{
   const response=await fetch(session.url,{method:"GET",signal:AbortSignal.timeout(30000)});
   if(!response.ok)throw new Error("Room poll failed ("+response.status+")");
   const body=await response.text();
   for(const raw of body.split("\x1e")){
    if(!raw)continue;
    let frame;try{frame=JSON.parse(raw)}catch{continue}
    if(frame.type===1&&frame.target==="PlaybackChanged"){
     const p=frame.arguments?.[0];if(p&&Number.isFinite(p.position))chrome.tabs.sendMessage(session.tabId,{type:"INSYNC_APPLY",position:p.position,playing:p.playing}).catch(()=>{});
    }
    if(frame.type===7)throw new Error("Room disconnected");
   }
  }catch(e){session.error=String(e);break}
 }
 if(sessions.get(session.tabId)===session){sessions.delete(session.tabId);chrome.action.setBadgeText({tabId:session.tabId,text:"!"}).catch(()=>{})}
}
async function invoke(session,target,args){
 const response=await fetch(session.url,{method:"POST",headers:{"Content-Type":"text/plain;charset=UTF-8"},body:hubFrame(target,args)});
 if(!response.ok)throw new Error("Room command failed ("+response.status+")");
}
async function connect(tabId,roomCode,name){
 const previous=sessions.get(tabId);if(previous)sessions.delete(tabId);
 const token=await negotiate();
 const url=API+"/hubs/rooms?id="+encodeURIComponent(token);
 const session={tabId,roomCode,name,url,error:""};
 sessions.set(tabId,session);
 const handshake=await fetch(url,{method:"POST",headers:{"Content-Type":"text/plain;charset=UTF-8"},body:'{"protocol":"json","version":1}\x1e'});
 if(!handshake.ok){sessions.delete(tabId);throw new Error("SignalR handshake failed")}
 await invoke(session,"JoinRoom",[roomCode,name]);
 chrome.action.setBadgeText({tabId,text:"ON"}).catch(()=>{});
 void poll(session);
 return {connected:true};
}
chrome.runtime.onMessage.addListener((message,sender,sendResponse)=>{
 (async()=>{
  if(message?.type==="INSYNC_CONNECT"){sendResponse(await connect(message.tabId,message.roomCode,message.name));return}
  if(message?.type==="INSYNC_PLAYBACK"){
   const session=sessions.get(sender.tab?.id);
   if(session&&session.roomCode===message.roomCode)await invoke(session,"PlaybackChanged",[session.roomCode,message.state.position,message.state.playing]);
   sendResponse({ok:true});return
  }
  sendResponse({ok:false});
 })().catch(e=>sendResponse({error:String(e)}));
 return true;
});
chrome.tabs.onRemoved.addListener(tabId=>sessions.delete(tabId));
