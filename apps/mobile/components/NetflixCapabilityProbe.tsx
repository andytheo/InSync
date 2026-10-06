import { useMemo,useState } from "react";
import { ActivityIndicator,Pressable,StyleSheet,Text,View } from "react-native";
import { WebView } from "react-native-webview";

export type NetflixProbeStage="opening"|"login"|"browse"|"watch"|"blocked"|"error";
const netflixHttps=(value:string)=>{try{const u=new URL(value);return u.protocol==="https:"&&(u.hostname==="netflix.com"||u.hostname.endsWith(".netflix.com"));}catch{return false}};
const stageFor=(value:string):NetflixProbeStage=>{try{const u=new URL(value);const p=u.pathname.toLowerCase();if(p.startsWith("/login"))return "login";if(p.startsWith("/watch/"))return "watch";if(p.startsWith("/browse")||p.startsWith("/title/"))return "browse";return "browse"}catch{return "error"}};

export default function NetflixCapabilityProbe({initialUrl,onNavigation,onStage}:{initialUrl?:string;onNavigation?:(url:string)=>void;onStage?:(stage:NetflixProbeStage)=>void}){
 const [loading,setLoading]=useState(true),[message,setMessage]=useState(""),[stage,setStage]=useState<NetflixProbeStage>("opening"),[key,setKey]=useState(0);
 const source=useMemo(()=>({uri:initialUrl&&netflixHttps(initialUrl)?initialUrl:"https://www.netflix.com/login"}),[initialUrl,key]);
 const update=(url:string)=>{const next=stageFor(url);setStage(next);onStage?.(next);onNavigation?.(url)};
 const fail=(text:string)=>{setLoading(false);setStage("error");onStage?.("error");setMessage(text)};
 return <View style={s.wrap}>
  <View style={s.bar}><Text style={s.stage}>NETFLIX · {stage.toUpperCase()}</Text><Pressable onPress={()=>{setMessage("");setLoading(true);setStage("opening");setKey(v=>v+1)}}><Text style={s.retry}>Reload</Text></Pressable></View>
  <View style={s.web}>
   {loading?<View pointerEvents="none" style={s.loading}><ActivityIndicator/><Text style={s.loadingText}>Opening Netflix securely…</Text></View>:null}
   <WebView key={key} source={source} javaScriptEnabled domStorageEnabled sharedCookiesEnabled thirdPartyCookiesEnabled allowsInlineMediaPlayback mediaPlaybackRequiresUserAction={false} setSupportMultipleWindows={false}
    originWhitelist={["https://*.netflix.com","https://netflix.com"]}
    onShouldStartLoadWithRequest={r=>netflixHttps(r.url)}
    onNavigationStateChange={e=>update(e.url)}
    onLoadEnd={e=>{setLoading(false);update(e.nativeEvent.url)}}
    onError={e=>fail(e.nativeEvent.description||"Netflix could not load in this embedded surface.")}
    onHttpError={e=>{if(e.nativeEvent.statusCode>=400)fail(`Netflix returned HTTP ${e.nativeEvent.statusCode}.`)}}
   />
  </View>
  {stage==="watch"?<View style={s.watchNotice}><Text style={s.watchText}>Netflix reached a /watch title inside InSync. Try Play. If protected playback is refused, stop there — that identifies the provider/DRM boundary.</Text></View>:null}
  {message?<View style={s.notice}><Text style={s.noticeText}>{message}</Text></View>:null}
 </View>
}
const s=StyleSheet.create({wrap:{width:"100%",height:570,overflow:"hidden",borderRadius:14,backgroundColor:"#000"},bar:{height:42,paddingHorizontal:12,flexDirection:"row",alignItems:"center",justifyContent:"space-between",backgroundColor:"#111827"},stage:{color:"#cbd5e1",fontSize:11,fontWeight:"900",letterSpacing:1},retry:{color:"#9aa8ff",fontWeight:"800",fontSize:12},web:{flex:1},loading:{...StyleSheet.absoluteFillObject,zIndex:2,alignItems:"center",justifyContent:"center",gap:10,backgroundColor:"#090e1b"},loadingText:{color:"#dce3f3",fontWeight:"700"},watchNotice:{padding:10,backgroundColor:"#14271f"},watchText:{color:"#9de1b5",fontSize:11,lineHeight:16,fontWeight:"700"},notice:{padding:10,backgroundColor:"#2d1720"},noticeText:{color:"#ffb4c2",fontWeight:"700",fontSize:12}});