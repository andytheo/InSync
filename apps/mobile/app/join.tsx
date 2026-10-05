import { useState } from "react";
import { router } from "expo-router";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";

export default function Join(){
 const [code,setCode]=useState("");const [name,setName]=useState("");
 return <View style={s.page}><Text style={s.kicker}>JOIN YOUR PERSON</Text><Text style={s.title}>Enter the room</Text><Text style={s.copy}>Use the private eight-character code your partner shared with you.</Text><TextInput placeholder="Room code" placeholderTextColor="#7f899f" value={code} onChangeText={x=>setCode(x.toUpperCase().replace(/[^A-Z2-9]/g,"").slice(0,8))} autoCapitalize="characters" autoCorrect={false} style={s.input}/><TextInput placeholder="Your name" placeholderTextColor="#7f899f" value={name} onChangeText={setName} style={s.input}/><Pressable disabled={code.length!==8} onPress={()=>router.replace({pathname:"/room/[code]",params:{code,name:name.trim()||"Guest"}})} style={[s.button,code.length!==8&&s.disabled]}><Text style={s.buttonText}>Join room</Text></Pressable></View>
}
const s=StyleSheet.create({page:{flex:1,padding:28,paddingTop:60,gap:16,backgroundColor:"#0b1020"},kicker:{color:"#8da2fb",fontWeight:"800",letterSpacing:2},title:{color:"white",fontSize:34,fontWeight:"800"},copy:{color:"#aeb7cc",fontSize:16,lineHeight:23},input:{backgroundColor:"#171e33",borderWidth:1,borderColor:"#29324b",borderRadius:14,padding:16,color:"white",fontSize:17},button:{backgroundColor:"#7c8cff",borderRadius:14,padding:17},disabled:{opacity:.4},buttonText:{textAlign:"center",fontWeight:"800",fontSize:17,color:"#08101f"}});