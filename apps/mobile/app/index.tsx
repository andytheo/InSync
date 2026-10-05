import { Link } from "expo-router";
import { ScrollView,StyleSheet,Text,View } from "react-native";

export default function Home(){
 return <ScrollView contentContainerStyle={s.page}>
  <View style={s.mark}><Text style={s.markText}>∞</Text></View>
  <View style={s.hero}><Text style={s.eyebrow}>INSYNC</Text><Text style={s.title}>Watch together.{"\n"}Even when apart.</Text><Text style={s.copy}>A private room for two. Pick a YouTube video and InSync keeps both phones watching the same moment.</Text></View>
  <View style={s.actions}><Link href="/create" style={[s.button,s.primary]}>Create a room</Link><Link href="/join" style={[s.button,s.secondary]}>Join with a code</Link></View>
  <Text style={s.note}>Private by design · Two people · No account required</Text>
 </ScrollView>
}
const s=StyleSheet.create({page:{flexGrow:1,padding:28,paddingTop:88,paddingBottom:42,backgroundColor:"#090e1b"},mark:{width:58,height:58,borderRadius:18,backgroundColor:"#7c8cff",alignItems:"center",justifyContent:"center",marginBottom:34},markText:{fontSize:36,fontWeight:"900",color:"#08101f"},hero:{gap:15},eyebrow:{color:"#8da2fb",fontWeight:"900",letterSpacing:3,fontSize:13},title:{color:"white",fontSize:43,lineHeight:48,fontWeight:"900"},copy:{color:"#aeb8cd",fontSize:17,lineHeight:26,maxWidth:440},actions:{gap:12,marginTop:38},button:{overflow:"hidden",padding:18,borderRadius:16,textAlign:"center",fontSize:17,fontWeight:"800"},primary:{backgroundColor:"#7c8cff",color:"#08101f"},secondary:{backgroundColor:"#151c30",color:"white",borderWidth:1,borderColor:"#283149"},note:{color:"#687287",textAlign:"center",marginTop:22,fontSize:12}});
