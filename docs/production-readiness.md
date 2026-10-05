# InSync production readiness

## V1 release scope
Private two-person rooms, YouTube embedded playback, synchronized play/pause/seek, reactions, reconnect/resync, and room expiry. Do not add public rooms or unsupported streaming-provider integrations before V1.

## Production runtime contract
The API is container-ready and listens on port 8080. Production startup intentionally fails unless both of these settings exist:

- `ConnectionStrings__InSync`: PostgreSQL connection string.
- `AllowedOrigins`: comma-separated trusted web origins. Native clients are not protected by CORS; this prevents accidentally exposing browser access to arbitrary origins.

The mobile production build intentionally fails fast unless `EXPO_PUBLIC_API_URL` is configured and uses HTTPS.

## Deployment gate
1. Provision managed PostgreSQL with encrypted connections, backups, and credentials stored in the host's secret manager.
2. Deploy `backend/InSync.Api/Dockerfile` behind HTTPS/WSS.
3. Set `ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__InSync`, and `AllowedOrigins`.
4. Verify `GET /health` and `GET /ready`; readiness must report a connected database.
5. Build mobile with `EXPO_PUBLIC_API_URL=https://<production-api-host>`.
6. Test iPhone and Android on different networks, including cellular, reconnect/background/foreground, invalid links, changing videos, and 30–60 minute sessions.
7. Run concurrency/load tests before public beta. Current V1 architecture should initially deploy as a single API instance because room membership and SignalR groups are held in process. Horizontal scaling requires a distributed room-state strategy and SignalR backplane/service first.

## Protection already in place
- Production refuses to run without PostgreSQL.
- Production CORS is allow-listed.
- Room creation is rate-limited to 20 requests/minute per client partition.
- Room reads are rate-limited to 120 requests/minute per client partition.
- SignalR messages are capped at 32 KiB.
- Detailed SignalR errors are development-only.
- Room commands require a joined connection.
- Playback positions, names, URLs, media metadata, and reactions are bounded/validated.
- Inactive rooms expire and persisted state is deleted.
- CI runs backend integration tests and TypeScript checks.

## Remaining release blockers
- Secure non-guessable invite capability instead of relying only on a six-digit code.
- Host/room ownership lifecycle.
- Production error monitoring and privacy-safe operational metrics.
- Load test the deployed topology and establish capacity limits.
- App icon/splash/accessibility/store metadata.
- Privacy policy, terms, support URL, and store privacy/data-safety declarations.
- TestFlight and Google Play closed testing.
