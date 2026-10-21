# Proposal

## Why

There is no way to check the selected microphone without a second player: voice capture is disabled outside multiplayer, the HUD only reports the voice-chat state, and incoming playback does not include the local voice. A player who suspects a broken device, wrong input or bad level has to host a second session and ask someone else to listen.

## What Changes

- Add an explicit **Microphone test mode** that can be started and stopped from Generic Mod Config Menu and a configurable keybind (default `F9`).
- While the test mode is active in a loaded world, capture the selected input locally, show an input-level meter in the HUD and optionally monitor the microphone through the selected output device.
- Never transmit test audio: test mode produces no voice packets and does not affect other players.
- Keep normal voice chat paused while test mode is active and restore the previous microphone/playback state when it is disabled.
- Add settings for the test keybind and the optional local monitor; the monitor uses `PlaybackVolume` (the same master volume as incoming voice), so a player can validate the volume control locally. Monitoring is disabled by default to avoid feedback.
- Stop the test automatically when the player leaves the world, returns to the title screen or disables it. The active state is never persisted and nothing is recorded to disk.
- Document the privacy behavior and the headphone recommendation for monitoring.
- This is additive; no wire-format or existing voice-chat behavior changes when test mode is off.

## Capabilities

### New Capabilities

- `microphone-test`: local, opt-in microphone testing with level feedback, optional sidetone monitoring and strict no-transmission/privacy rules.

### Modified Capabilities

- `voice-capture`: adds the controlled exception that a loaded single-player world may open the microphone while microphone test mode is explicitly active; normal capture still never transmits in single-player.
- `voice-chat-settings`: exposes the test keybind and local-monitor toggle, and includes them in reset-to-defaults.

## Impact

- **Code**: `Config/ModConfig.cs`, `ModEntry.cs`, a test-mode controller around `AudioEngine`, `UI/VoiceIndicator.cs`/indicator rules, `Integrations/GmcmIntegration.cs`, tests and `README.md`.
- **Settings**: two additive fields; existing configs remain valid.
- **Audio**: test mode takes over the capture stream and, when monitoring, opens a local playback path; normal voice is paused while it is active to avoid two consumers competing for the same device.
- **Privacy**: test audio never leaves the machine, is never written to disk, and test mode is visibly active. Monitoring is opt-in and may cause feedback without headphones.
- **Network**: no packets are sent in test mode; no protocol change.
