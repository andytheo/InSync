import { Link } from "expo-router";
import { ScrollView, StyleSheet, Text, View } from "react-native";

export default function Home() {
  return (
    <ScrollView contentContainerStyle={s.page}>
      <View style={s.hero}>
        <Text style={s.eyebrow}>INSYNC</Text>
        <Text style={s.title}>Date night, wherever you are.</Text>
        <Text style={s.copy}>Start a private room, invite your person, and keep the moment in sync.</Text>
      </View>
      <Link href="/create" style={[s.button, s.primary]}>Create a room</Link>
      <Link href="/join" style={[s.button, s.secondary]}>Join a room</Link>
      <Text style={s.note}>Private rooms · shared controls · lightweight reactions</Text>
    </ScrollView>
  );
}
const s=StyleSheet.create({
  page:{flexGrow:1,padding:28,paddingTop:72,gap:16,backgroundColor:"#0b1020"},
  hero:{gap:14,marginBottom:20},eyebrow:{color:"#8da2fb",fontWeight:"800",letterSpacing:3},
  title:{color:"white",fontSize:42,lineHeight:47,fontWeight:"800"},copy:{color:"#c7cee0",fontSize:17,lineHeight:25},
  button:{overflow:"hidden",padding:18,borderRadius:16,textAlign:"center",fontSize:17,fontWeight:"700"},
  primary:{backgroundColor:"#7c8cff",color:"#08101f"},secondary:{backgroundColor:"#171e33",color:"white"},
  note:{color:"#7f899f",textAlign:"center",marginTop:12}
});