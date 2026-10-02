import { useState } from "react";
import { router } from "expo-router";
import { Button, Text, TextInput, View } from "react-native";
import { createRoom } from "../lib/insync";

export default function Create() {
  const [name, setName] = useState("");
  const [error, setError] = useState("");
  async function submit() {
    try {
      setError("");
      const room = await createRoom(name.trim() || "Host");
      router.replace({ pathname: "/room/[code]", params: { code: room.code, name: name.trim() || "Host" } });
    } catch (e) { setError(e instanceof Error ? e.message : "Could not create room."); }
  }
  return <View style={{ padding: 24, gap: 12 }}><Text>Create a room</Text><TextInput placeholder="Your name" value={name} onChangeText={setName} style={{ borderWidth: 1, padding: 12 }} /><Button title="Create" onPress={submit} />{error ? <Text>{error}</Text> : null}</View>;
}
