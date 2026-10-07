const room=document.querySelector("#room"),name=document.querySelector("#name"),status=document.querySelector("#status");
chrome.storage.local.get(["roomCode","name"],x=>{room.value=x.roomCode||"";name.value=x.name||""});
document.querySelector("#connect").addEventListener("click",async()=>{
 const roomCode=room.value.trim().toUpperCase(),displayName=name.value.trim()||"Guest";
 if(!/^[A-Z0-9]{8}$/.test(roomCode)){status.textContent="Enter the 8-character InSync room code.";return;}
 const [tab]=await chrome.tabs.query({active:true,currentWindow:true});
 if(!tab?.id||!/^https:\/\/www\.netflix\.com\//i.test(tab.url||"")){status.textContent="Open netflix.com in the active tab first.";return;}
 await chrome.storage.local.set({roomCode,name:displayName});
 await chrome.tabs.sendMessage(tab.id,{type:"INSYNC_ROOM",roomCode,name:displayName});
 status.textContent="Connected locally. Realtime room transport is the next adapter step.";
});
