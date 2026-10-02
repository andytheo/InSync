import { useState } from "react";
import { router } from "expo-router";
import { Button, Text, TextInput, View } from "react-native";

export default function Join() {
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  return <View style={{ padding: 24, gap: 12 }}><Text>Join a room</Text><TextInput placeholder="Room code" value={code} onChangeText={setCode} autoCapitalize="characters" style={{ borderWidth: 1, padding: 12 }} /><TextInput placeholder="Your name" value={name} onChangeText={setName} style={{ borderWidth: 1, padding: 12 }} /><Button title="Join" disabled={!code.trim()} onPress={() => router.replace({ pathname: "/room/[code]", params: { code: code.trim(), name: name.trim() || "Guest" } })} /></View>;
}
