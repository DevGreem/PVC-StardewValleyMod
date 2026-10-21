# Proposal

## Why

The security audit found that voice packets are broadcast to every peer even though proximity only gates playback, that spoofed sender IDs can grow rate-limit state without bound, and that oversized Base64 payloads are decoded before their size is checked. It also found smaller input-handling and documentation risks. These should be fixed before adding new proximity features.

## What Changes

- Limit outgoing voice packets to peers in the same location and inside the declared reach instead of broadcasting to the session. Document that this is a mitigation, not confidentiality against a modified client or the host.
- Reject voice messages from IDs that are not current farmers before creating per-sender rate-limit state.
- Reject Base64 payloads above the maximum valid encoded packet length before decoding them.
- Add a global rate limit for wall-occlusion settings messages.
- Ignore PVC keybinds and force push-to-talk off while the chat box is active.
- Sanitize remote player names before displaying, logging or persisting them.
- Correct README/spec claims about proximity privacy and sender authentication.
- **Accepted risk:** application-level authentication of `FromPlayerID` remains deferred. SMAPI does not authenticate it; the mod will document that voice attribution and local mute are advisory.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `voice-transport`: recipient filtering, unknown-sender rejection and pre-decode size bounds.
- `wall-occlusion`: rate-limit settings requests.
- `voice-chat-settings`: keybinds and push-to-talk are inactive while typing in chat.
- `player-mute`: remote names are sanitized before display and persistence.

## Impact

- **Code**: `VoiceTransport`, `PacketValidator`/packet limits, `ModEntry` input and wall-occlusion handling, name sanitization, README and tests.
- **Network**: outgoing voice packets become unicast/multicast to eligible peers rather than broadcast; packet format is unchanged.
- **Security**: closes the accidental full-session broadcast for honest clients, bounds state/allocation from forged IDs and oversized payloads, and hardens input handling. It does not make proximity a cryptographic confidentiality boundary.
- **Compatibility**: no wire-format change; mixed mod versions continue to exchange the same voice packet format, subject to the existing radius rules.
- **Tests**: pure recipient-selection tests, encoded-size tests, unknown-sender/rate-limit tests, name sanitization tests, and existing suite/build.
