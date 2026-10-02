# InSync

A private, cross-platform watch-together companion for people who are apart.

## Current milestone

Two real phones can create/join the same room and synchronize playback state through the ASP.NET Core + SignalR backend. The mobile app now includes embedded YouTube playback, room presence, shared video selection, ready gating, a server-authoritative synchronized countdown, play/pause/seek/scrub synchronization, drift correction, reactions, and reconnect state restoration. The remaining gate for this milestone is physical iPhone/Android WebView validation.

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
- [x] Fix participant identity so reconnects do not create duplicates
- [x] Track disconnects and remove/offline participants correctly
- [x] Make playback sequence updates concurrency-safe
- [x] Add room expiration and cleanup
- [x] Add API health endpoint and connection diagnostics
- [x] Integration tests for playback, reconnect, cleanup, authorization, media, ready/content/reactions

### P1 — Persistent, secure rooms
- [x] PostgreSQL + EF Core persistence
- [x] Connection-bound guest identity and room command authorization
- [ ] Non-guessable invite links/tokens
- [ ] Host permissions and room lifecycle
- [ ] Rate limiting and abuse protection
- [ ] Production HTTPS configuration
- [ ] Minimal privacy-safe telemetry and error reporting

### P2 — Real watch-together experience
- [x] Provider-neutral content model (title, provider, deep link)
- [x] Open supported streaming links without collecting provider passwords
- [x] Server-authoritative start-together countdown
- [x] Real playback timeline and drift correction
- [ ] Provider capability detection
- [x] YouTube embedded playback bridge with play/pause/seek/scrub synchronization (physical-device validation pending)
- [ ] Investigate/implement official Apple SharePlay integration
- [ ] Netflix / Prime Video / Disney+ / Hulu: integrate only through supported APIs, deep links, or platform capabilities; never bypass DRM

### P3 — Date-night features
- [ ] In-room text chat
- [ ] Managed voice/video calling integration
- [ ] Content voting / watchlist
- [ ] Scheduled date nights and reminders
- [ ] Room history / favorites

### P4 — Release
- [x] Automated mobile/backend CI gates
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
