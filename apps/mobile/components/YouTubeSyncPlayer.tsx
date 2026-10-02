import { useEffect,useMemo,useRef } from "react";
import { StyleSheet,View } from "react-native";
import { WebView,WebViewMessageEvent } from "react-native-webview";
import type { Playback } from "../lib/insync";

export function youtubeVideoId(value:string){const v=value.trim();if(/^[\w-]{11}$/.test(v))return v;const m=v.match(/(?:youtu\.be\/|youtube\.com\/(?:watch\?v=|embed\/|shorts\/))([\w-]{11})/i);return m?.[1]??null;}
export default function YouTubeSyncPlayer({url,playback,onLocalPlayback}:{url:string;playback:Playback;onLocalPlayback:(position:number,playing:boolean)=>void}){
 const ref=useRef<WebView>(null),applying=useRef(false),ready=useRef(false),lastSent=useRef({position:-1,playing:false,at:0});const id=useMemo(()=>youtubeVideoId(url),[url]);
 const html=useMemo(()=>id?`<!doctype html><html><head><meta name="viewport" content="width=device-width,initial-scale=1"/></head><body style="margin:0;background:#000"><div id="p"></div><script src="https://www.youtube.com/iframe_api"></script><script>let p;function send(type){if(!p)return;ReactNativeWebView.postMessage(JSON.stringify({type,position:p.getCurrentTime(),playing:p.getPlayerState()===1}))}function onYouTubeIframeAPIReady(){p=new YT.Player("p",{videoId:"${id}",width:"100%",height:"100%",playerVars:{playsinline:1,controls:1},events:{onReady:()=>send("ready"),onStateChange:()=>send("state")}})}window.applyState=(pos,playing)=>{if(!p)return;p.seekTo(pos,true);playing?p.playVideo():p.pauseVideo()}</script></body></html>`:"",[id]);
 useEffect(()=>{if(!id||!ready.current)return;applying.current=true;ref.current?.injectJavaScript(`window.applyState(${Math.max(0,playback.position)},${playback.playing});true;`);const t=setTimeout(()=>applying.current=false,600);return()=>clearTimeout(t)},[id,playback.sequence]);
 if(!id)return null;const message=(e:WebViewMessageEvent)=>{try{const x=JSON.parse(e.nativeEvent.data);if(x.type==="ready"){ready.current=true;applying.current=true;ref.current?.injectJavaScript(`window.applyState(${Math.max(0,playback.position)},${playback.playing});true;`);setTimeout(()=>applying.current=false,600);return}if(x.type==="state"&&!applying.current){const position=Number(x.position)||0,playing=!!x.playing,now=Date.now(),last=lastSent.current;if(playing!==last.playing||Math.abs(position-last.position)>1.5||now-last.at>1500){lastSent.current={position,playing,at:now};onLocalPlayback(position,playing)}}}catch{}};
 return <View style={s.wrap}><WebView ref={ref} source={{html,baseUrl:"https://www.youtube.com"}} javaScriptEnabled allowsInlineMediaPlayback mediaPlaybackRequiresUserAction={false} onMessage={message}/></View>;
}
const s=StyleSheet.create({wrap:{width:"100%",aspectRatio:16/9,minHeight:200,overflow:"hidden",borderRadius:14,backgroundColor:"#000"}});
