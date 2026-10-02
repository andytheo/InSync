import { useEffect, useRef, useState } from "react";
import { useLocalSearchParams } from "expo-router";
import { Button, Text, TextInput, View } from "react-native";
import type { HubConnection } from "@microsoft/signalr";
import { connectToRoom, Playback } from "../../lib/insync";

export default function RoomScreen() {
  const params = useLocalSearchParams<{ code: string; name: string }>();
  const code = String(params.code);
  const name = String(params.name ?? "Guest");
  const connection = useRef<HubConnection | null>(null);
  const [status, setStatus] = useState("Connecting");
  const [playback, setPlayback] = useState<Playback>({ position: 0, playing: false, sequence: 0, updatedAt: "" });
  const [position, setPosition] = useState("0");

  useEffect(() => {
    let active = true;
    connectToRoom(code, name, state => { if (active) { setPlayback(state); setPosition(String(Math.round(state.position))); } })
      .then(c => { if (active) { connection.current = c; setStatus("Connected"); } else c.stop(); })
      .catch(() => setStatus("Connection failed"));
    return () => { active = false; connection.current?.stop(); };
  }, [code, name]);

  async function change(playing: boolean, pos = playback.position) {
    await connection.current?.invoke("PlaybackChanged", code, Math.max(0, pos), playing);
  }

  const seek = Number(position);
  return <View style={{ padding: 24, gap: 12 }}><Text style={{ fontSize: 24, fontWeight: "700" }}>Room {code}</Text><Text>{status}</Text><Text>{playback.playing ? "Playing" : "Paused"} · {playback.position.toFixed(1)}s · seq {playback.sequence}</Text><Button title="Play" disabled={status !== "Connected"} onPress={() => change(true)} /><Button title="Pause" disabled={status !== "Connected"} onPress={() => change(false)} /><TextInput keyboardType="numeric" value={position} onChangeText={setPosition} style={{ borderWidth: 1, padding: 12 }} /><Button title="Seek" disabled={status !== "Connected" || !Number.isFinite(seek)} onPress={() => change(playback.playing, seek)} /></View>;
}
