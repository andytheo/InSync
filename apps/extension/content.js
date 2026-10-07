// InSync Netflix adapter — intentionally minimal and independently designed.
// It observes only ordinary HTML5 media state. It never reads credentials,
// cookies, storage, manifests, DRM licenses/keys, or media bytes.
const API="https://insync-api-tlfk.onrender.com";
let roomCode="", participantName="", suppressUntil=0, lastVideo=null;

function findVideo(){ return document.querySelector("video"); }
function snapshot(video){ return {position:Number(video.currentTime||0),playing:!video.paused}; }

async function sendPlayback(video){
 if(!roomCode||Date.now()<suppressUntil)return;
 const state=snapshot(video);
 chrome.runtime.sendMessage({type:"INSYNC_PLAYBACK",roomCode,state});
}
function bind(){
 const video=findVideo();
 if(!video||video===lastVideo)return;
 lastVideo=video;
 ["play","pause","seeked"].forEach(evt=>video.addEventListener(evt,()=>sendPlayback(video),{passive:true}));
 chrome.runtime.sendMessage({type:"INSYNC_PLAYER_READY",roomCode,url:location.href});
}
new MutationObserver(bind).observe(document.documentElement,{subtree:true,childList:true});
bind();

chrome.runtime.onMessage.addListener((msg)=>{
 if(msg?.type==="INSYNC_ROOM"){roomCode=String(msg.roomCode||"");participantName=String(msg.name||"");bind();}
 if(msg?.type==="INSYNC_APPLY"&&lastVideo){
   suppressUntil=Date.now()+1200;
   const p=Number(msg.position);
   if(Number.isFinite(p)&&Math.abs(lastVideo.currentTime-p)>1.25)lastVideo.currentTime=p;
   if(msg.playing&&lastVideo.paused)lastVideo.play().catch(()=>{});
   if(!msg.playing&&!lastVideo.paused)lastVideo.pause();
 }
});
