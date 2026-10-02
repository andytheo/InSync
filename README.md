# InSync

A private, cross-platform watch-together companion for people who are apart.

## Current milestone

Two real phones can create/join the same room and synchronize playback state through the ASP.NET Core + SignalR backend. The mobile app now includes the first date-night UI, room presence, shared content selection, ready state, reactions, and synchronized play/pause/seek controls.

## Roadmap

### P0 — Make the core reliable
- [x] Create a private six-digit room
- [x] Join from a second physical phone
- [x] Server-authoritative play/pause/seek synchronization
- [x] Participant presence
- [x] Shared content selection
- [x] Ready state
- [x] Reactions
- [x] Mobile-first v0.1 UI
- [ ] Fix participant identity so reconnects do not create duplicates
- [ ] Track disconnects and remove/offline participants correctly
- [ ] Make playback sequence updates concurrency-safe
- [ ] Add room expiration and cleanup
- [ ] Add API health endpoint and connection diagnostics
- [ ] Expand integration tests for ready/content/reactions/reconnects

### P1 — Persistent, secure rooms
- [ ] PostgreSQL + EF Core persistence
- [ ] User/guest identity and secure room membership
- [ ] Non-guessable invite links/tokens
- [ ] Host permissions and room lifecycle
- [ ] Rate limiting and abuse protection
- [ ] Production HTTPS configuration
- [ ] Minimal privacy-safe telemetry and error reporting

### P2 — Real watch-together experience
- [ ] Provider-neutral content model (title, provider, deep link)
- [ ] Open supported streaming apps without collecting provider passwords
- [ ] Start-together countdown
- [ ] Real playback timeline and drift correction
- [ ] Provider capability detection
- [ ] YouTube-supported playback integration
- [ ] Investigate/implement official Apple SharePlay integration
- [ ] Netflix / Prime Video / Disney+ / Hulu: integrate only through supported APIs, deep links, or platform capabilities; never bypass DRM

### P3 — Date-night features
- [ ] In-room text chat
- [ ] Managed voice/video calling integration
- [ ] Content voting / watchlist
- [ ] Scheduled date nights and reminders
- [ ] Room history / favorites

### P4 — Release
- [ ] Automated mobile/backend CI gates
- [ ] Production API/database deployment
- [ ] App icons, splash screen, accessibility and polish
- [ ] Privacy policy / terms / account deletion
- [ ] TestFlight beta
- [ ] Android closed beta
- [ ] App Store / Play Store release readiness

## Development

Backend:

```powershell
cd backend/InSync.Api
dotnet run --urls "http://0.0.0.0:5000"
```

Mobile on the current development Wi-Fi:

```powershell
cd apps/mobile
npm run start:phone
```

The current phone script is for local development only. Production builds will use a deployed HTTPS API URL.
