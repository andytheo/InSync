// InSync independent desktop adapter: ordinary HTML5 playback events only.
// Never inspect credentials, cookies, DRM, manifests, or media bytes.
let roomCode="",lastVideo=null,suppressUntil=0,lastSent="",lastSentAt=0;
function findVideo(){return document.querySelector("video")}
function emit(video){
 if(!roomCode||Date.now()<suppressUntil)return;
 const position=Number(video.currentTime);
 if(!Number.isFinite(position))return;
 const playing=!video.paused;
 const signature=playing+":"+Math.round(position*2);
 if(signature===lastSent&&Date.now()-lastSentAt<1500)return;
 lastSent=signature;lastSentAt=Date.now();
 chrome.runtime.sendMessage({type:"INSYNC_PLAYBACK",roomCode,state:{position,playing}}).catch(()=>{});
}
function bind(){
 const video=findVideo();
 if(!video||video===lastVideo)return;
 lastVideo=video;
 ["play","pause","seeked"].forEach(event=>video.addEventListener(event,()=>emit(video),{passive:true}));
}
new MutationObserver(bind).observe(document.documentElement,{subtree:true,childList:true});
bind();
chrome.runtime.onMessage.addListener((message,_,reply)=>{
 if(message?.type==="INSYNC_ROOM"){
  roomCode=String(message.roomCode||"");bind();reply({ok:true});return;
 }
 if(message?.type==="INSYNC_APPLY"){
  const video=findVideo();if(!video){reply({ok:false,reason:"No HTML5 player found"});return}
  suppressUntil=Date.now()+2200;
  const position=Number(message.position);
  if(Number.isFinite(position)&&position>=0&&Math.abs(video.currentTime-position)>1.5){
   try{video.currentTime=position}catch{}
  }
  if(message.playing&&!video.paused){}else if(message.playing)video.play().catch(()=>{});
  else if(!video.paused)video.pause();
  reply({ok:true});return;
 }
});
