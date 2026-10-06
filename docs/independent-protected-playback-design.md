# InSync protected playback design

InSync uses an independently designed provider-adapter boundary. It does not copy another watch-party application's source code, private APIs, visual design, branding, traffic, DRM credentials, cookies, or proprietary protocol.

## Product boundary

The InSync room owns presence, invitations, chat, reactions, and synchronization state. A provider adapter owns only its playback surface and reports playback capabilities that the provider permits.

YouTube uses the official iframe API. Netflix uses a provider-owned authenticated surface whose capabilities are being evaluated.

## Netflix experiment

The Android experiment embeds Netflix's own HTTPS surface in an isolated WebView. Credentials are submitted directly to Netflix. InSync does not inject scripts into Netflix pages, inspect form values, export cookies or storage, intercept authentication, extract manifests, inspect DRM licenses, obtain content keys, proxy media, or rebroadcast video.

The physical-device test answers three separate questions: whether authentication completes, whether authenticated catalog/title navigation completes, and whether Netflix permits protected title playback in that surface. Successful login is not treated as proof that protected playback is supported. If protected playback is rejected, the adapter stops at that boundary and requires a provider-authorized native integration rather than circumventing the restriction.

## Synchronization contract

Provider adapters may expose only ordinary playback state that their supported integration makes available: ready, playing/paused, current position, and user-initiated seek. InSync's server-authoritative SignalR state remains provider-neutral.

## Independence rules

Do not reverse engineer another watch-party application, reproduce its UI, capture its network traffic, reuse its assets, impersonate its clients, obtain its provider credentials, or reproduce undocumented proprietary behavior. Product similarities are limited to generic watch-together concepts such as private rooms, synchronized playback, chat, and reactions.
