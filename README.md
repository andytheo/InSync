# InSync

A private, cross-platform watch-together companion for two people who are apart.

## V1

InSync V1 is intentionally narrow: create a private room, invite one other person, paste a YouTube link, watch the embedded video on both phones, synchronize play/pause/seek, react, and recover after reconnects.

Two physical phones have already validated the core embedded YouTube synchronization flow. V1 does not host or rebroadcast video and does not collect streaming-service credentials.

## Release status

### Core experience
- [x] Two-person private rooms
- [x] Cryptographically generated eight-character invite codes
- [x] Connection-bound room authorization
- [x] Participant presence and reconnect restoration
- [x] Shared YouTube selection
- [x] Embedded YouTube playback on both physical phones
- [x] Server-authoritative play/pause/seek state
- [x] Drift correction and feedback-loop suppression
- [x] Fixed emoji reactions
- [x] Room expiration and cleanup
- [x] PostgreSQL + EF Core persistence

### Production hardening
- [x] API health and database readiness endpoints
- [x] Production requires PostgreSQL configuration
- [x] Production mobile builds require an HTTPS API
- [x] Production CORS allow-list
- [x] HTTP/SignalR connection rate limits
- [x] SignalR message-size limit
- [x] Two-person room capacity enforcement
- [x] Bounded/validated names, URLs, media, playback, and reactions
- [x] Docker production image + CI build verification
- [x] Backend integration tests + mobile TypeScript CI
- [x] Stable iOS/Android identifiers and EAS release profiles
- [ ] Deploy public HTTPS/WSS API + managed PostgreSQL
- [ ] Production monitoring/error reporting
- [ ] Load/capacity test deployed environment
- [ ] Cross-network iPhone/Android beta

### Store release
- [ ] App icon, splash and final accessibility pass
- [ ] Privacy policy, terms and support URL
- [ ] Store privacy/data-safety declarations
- [ ] TestFlight beta
- [ ] Google Play testing
- [ ] App Store / Play Store submission

## Architecture

- Mobile: Expo + React Native + TypeScript
- Realtime: ASP.NET Core SignalR
- API: ASP.NET Core / .NET 8
- Persistence: PostgreSQL + EF Core
- Video: official embedded YouTube player; video traffic does not pass through the InSync backend

The first production deployment should use one API instance. Room membership and SignalR groups currently live in process. Horizontal scaling requires distributed room state and a SignalR backplane/service before adding replicas.

## Development

Backend:

```powershell
cd backend/InSync.Api
dotnet run --urls "http://0.0.0.0:5000"
```

Mobile on development Wi-Fi:

```powershell
cd apps/mobile
npm run start:phone
```

Production builds use `EXPO_PUBLIC_API_URL=https://<production-api-host>`; localhost/LAN endpoints are development-only. See `docs/production-readiness.md`.
