import { useMemo,useState } from "react";
import { ActivityIndicator,StyleSheet,Text,View } from "react-native";
import { WebView } from "react-native-webview";

/**
 * Netflix capability probe.
 *
 * This deliberately treats netflix.com as an isolated provider-owned surface:
 * - credentials are entered only into Netflix's page;
 * - InSync does not inject JavaScript into Netflix pages;
 * - InSync does not read/export cookies, storage, passwords, manifests or DRM data;
 * - navigation is restricted to Netflix HTTPS origins.
 *
 * The purpose is to learn, on a physical Android device, how far Netflix's own
 * web client can progress in Android WebView before protected playback policy/
 * DRM support becomes the boundary.
 */
export default function NetflixCapabilityProbe({initialUrl,onNavigation}:{initialUrl?:string;onNavigation?:(url:string)=>void}){
 const [loading,setLoading]=useState(true);
 const [message,setMessage]=useState("");
 const source=useMemo(()=>({uri:initialUrl&&/^https:\/\/([a-z0-9-]+\.)?netflix\.com\//i.test(initialUrl)?initialUrl:"https://www.netflix.com/login"}),[initialUrl]);
 return <View style={s.wrap}>
  {loading?<View pointerEvents="none" style={s.loading}><ActivityIndicator/><Text style={s.loadingText}>Opening Netflix securely…</Text></View>:null}
  <WebView
   source={source}
   javaScriptEnabled
   domStorageEnabled
   sharedCookiesEnabled
   thirdPartyCookiesEnabled
   allowsInlineMediaPlayback
   mediaPlaybackRequiresUserAction={false}
   setSupportMultipleWindows={false}
   originWhitelist={["https://*.netflix.com","https://netflix.com"]}
   onShouldStartLoadWithRequest={r=>{try{const u=new URL(r.url);return u.protocol==="https:"&&(u.hostname==="netflix.com"||u.hostname.endsWith(".netflix.com"));}catch{return false;}}}
   onLoadEnd={e=>{setLoading(false);onNavigation?.(e.nativeEvent.url)}}
   onError={e=>{setLoading(false);setMessage(e.nativeEvent.description||"Netflix could not load in this embedded surface.")}}
   onHttpError={e=>{if(e.nativeEvent.statusCode>=400)setMessage(`Netflix returned HTTP ${e.nativeEvent.statusCode}.`)}}
  />
  {message?<View style={s.notice}><Text style={s.noticeText}>{message}</Text></View>:null}
 </View>
}
const s=StyleSheet.create({wrap:{width:"100%",height:520,overflow:"hidden",borderRadius:14,backgroundColor:"#000"},loading:{...StyleSheet.absoluteFillObject,zIndex:2,alignItems:"center",justifyContent:"center",gap:10,backgroundColor:"#090e1b"},loadingText:{color:"#dce3f3",fontWeight:"700"},notice:{position:"absolute",left:10,right:10,bottom:10,padding:10,borderRadius:10,backgroundColor:"#2d1720"},noticeText:{color:"#ffb4c2",fontWeight:"700",fontSize:12}});