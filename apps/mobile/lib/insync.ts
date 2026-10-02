import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";

export type Room = { code: string; playback: Playback };
export type Playback = { position: number; playing: boolean; sequence: number; updatedAt: string };

const baseUrl = process.env.EXPO_PUBLIC_API_URL ?? "http://localhost:5000";

export async function createRoom(name: string): Promise<Room> {
  const response = await fetch(`${baseUrl}/api/rooms`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name }),
  });
  if (!response.ok) throw new Error("Could not create room.");
  return response.json();
}

export async function connectToRoom(code: string, name: string, onPlayback: (state: Playback) => void): Promise<HubConnection> {
  const connection = new HubConnectionBuilder().withUrl(`${baseUrl}/hubs/rooms`).withAutomaticReconnect().build();
  connection.on("PlaybackChanged", onPlayback);
  await connection.start();
  await connection.invoke("JoinRoom", code, name);
  return connection;
}
