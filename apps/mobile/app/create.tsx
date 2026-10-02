import { useState } from "react";
import { router } from "expo-router";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import { createRoom } from "../lib/insync";

export default function Create() {
  const [name,setName]=useState(""); const [error,setError]=useState(""); const [busy,setBusy]=useState(false);
  async function submit(){try{setBusy(true);setError("");const n=name.trim()||"Host";const room=await createRoom(n);router.replace({pathname:"/room/[code]",params:{code:room.code,name:n}});}catch(e){setError(e instanceof Error?e.message:"Could not create room.");}finally{setBusy(false);}}
  return <View style={s.page}><Text style={s.kicker}>HOST A DATE NIGHT</Text><Text style={s.title}>Create your room</Text><Text style={s.copy}>Give yourself a display name. We’ll create a private six-digit room to share.</Text><TextInput placeholder="Your name" placeholderTextColor="#7f899f" value={name} onChangeText={setName} style={s.input}/><Pressable disabled={busy} onPress={submit} style={s.button}><Text style={s.buttonText}>{busy?"Creating…":"Create room"}</Text></Pressable>{error?<Text style={s.error}>{error}</Text>:null}</View>;
}
const s=StyleSheet.create({page:{flex:1,padding:28,paddingTop:60,gap:16,backgroundColor:"#0b1020"},kicker:{color:"#8da2fb",fontWeight:"800",letterSpacing:2},title:{color:"white",fontSize:34,fontWeight:"800"},copy:{color:"#aeb7cc",fontSize:16,lineHeight:23,marginBottom:8},input:{backgroundColor:"#171e33",borderWidth:1,borderColor:"#29324b",borderRadius:14,padding:16,color:"white",fontSize:17},button:{backgroundColor:"#7c8cff",borderRadius:14,padding:17,marginTop:4},buttonText:{textAlign:"center",fontWeight:"800",fontSize:17,color:"#08101f"},error:{color:"#ff9a9a"}});