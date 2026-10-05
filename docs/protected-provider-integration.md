# Protected streaming providers: integration plan

## Goal
Keep the InSync room, chat, reactions, and synchronization experience while each participant watches through their own authorized streaming-service access.

## What the public products show
- Teleparty runs alongside the provider's own website/app and synchronizes playback state; it does not provide the subscription content itself.
- Rave publicly advertises in-app support for Netflix, Prime Video, Disney+ and other services, but its public documentation does not expose a general third-party playback API.
- Protected services use DRM. InSync must not extract manifests/keys, copy cookies or credentials, bypass DRM, or proxy/rebroadcast protected video.

## Architecture decision
YouTube remains the reference implementation because an official embeddable player exists.

For Netflix, Prime Video and Disney+, do not treat a normal React Native WebView as a production playback solution. A production in-app provider adapter requires a provider-authorized playback surface/license path.

Provider adapter contract:
1. authenticate through a provider-approved flow;
2. resolve a provider title using provider-supported identifiers;
3. render only through an authorized provider playback surface;
4. expose permitted play/pause/seek/time events to the InSync synchronization layer;
5. never send credentials, DRM licenses, manifests, or media bytes through the InSync backend.

## Work we can ship before provider authorization
- Keep room/chat/reactions/provider-selection infrastructure provider-neutral.
- Fix reconnect/membership recovery independently of provider playback.
- Add adapter boundaries so an authorized provider implementation can replace the temporary external handoff without changing room synchronization.
- Do not market Netflix/Prime/Disney as in-app playback until an authorized playback integration is actually working.

## Provider work required
Contact each provider/partner program for commercial playback integration terms and technical access. The key requirement is an authorized DRM/player integration, not a WebView workaround.

## Explicit non-goals
No DRM circumvention, stream extraction, credential/cookie interception, screen capture/rebroadcast, or pretending an external deep link is in-app playback.
