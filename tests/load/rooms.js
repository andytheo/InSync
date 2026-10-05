import http from "k6/http";
import ws from "k6/ws";
import { check, sleep } from "k6";

const base = (__ENV.BASE_URL || "").replace(/\/$/, "");
if (!base) throw new Error("Set BASE_URL to the deployed HTTPS API before running this test.");

export const options = {
  scenarios: {
    room_reads: {
      executor: "ramping-vus",
      startVUs: 1,
      stages: [
        { duration: "30s", target: 10 },
        { duration: "1m", target: 25 },
        { duration: "30s", target: 0 }
      ]
    }
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<500"]
  }
};

export function setup() {
  const response = http.post(`${base}/api/rooms`, JSON.stringify({ name: "Load Test" }), {
    headers: { "Content-Type": "application/json" }
  });
  check(response, { "room created": r => r.status === 200 });
  return { code: response.json("code") };
}

export default function(data) {
  const response = http.get(`${base}/api/rooms/${data.code}`);
  check(response, { "room read succeeds": r => r.status === 200 });
  sleep(1);
}
