# InSync browser extension (experimental)

Manifest V3 desktop extension for the independently designed InSync provider-adapter architecture.

The Netflix content script observes only ordinary HTML5 media play/pause/seek state and can apply those same ordinary controls. It does not inspect credentials, cookies, local/session storage, manifests, DRM licenses or keys, and it does not copy or proxy media.

Current milestone: local Netflix desktop player detection and room attachment UI. The existing InSync SignalR room transport will be connected in the next step.

Development: load this directory as an unpacked extension in a Chromium browser. Do not publish or market Netflix compatibility until end-to-end behavior and applicable platform/provider requirements have been reviewed.
