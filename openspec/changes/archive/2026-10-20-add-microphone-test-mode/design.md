# Design

## Context

See `proposal.md`. `AudioEngine` already can open a capture stream and a playback stream, but `ModEntry` only opens capture when a multiplayer world is loaded and the normal microphone is enabled. The HUD has no input-level meter, and normal voice playback is driven by the remote-speaker mixer. A test mode must reuse the audio backend without sending anything or leaving a second capture stream competing with voice chat.

## Goals / Non-Goals

**Goals:**

- Let the player open the selected microphone locally in a loaded world and see its level.
- Keep test audio local: no packets, no remote playback, no recording.
- Optionally monitor the microphone through the selected output.
- Stop automatically and restore normal voice when the test ends.
- Keep the active state out of the settings file.

**Non-Goals:**

- No recording or playback of clips.
- No test mode at the title screen or without a loaded world.
- No sidetone during normal voice chat; monitoring exists only inside test mode.
- No noise suppression, gain normalization or automatic device calibration.
- No change to the voice packet, codec or transport.

## Decisions

### D1 — Test mode owns the audio streams while active

Enabling test mode suspends normal voice capture and remote playback, stops the normal capture stream, opens a local capture stream and, if monitoring is enabled, ensures a local playback stream (reusing the existing output stream when one is already running). Disabling test mode stops the test streams and restores the previous normal voice state if the world and settings still allow it.

- **Why:** one capture stream avoids two consumers competing for the same device, and suspending remote playback guarantees test audio cannot be confused with received voice.
- **Alternative rejected:** run test capture concurrently with normal voice. Device contention already appeared during split-screen tests, and it makes “is this my voice or a remote voice?” ambiguous.
- **Consequence:** enabling the test during a multiplayer session temporarily pauses voice chat; the HUD and log must say so.

### D2 — A loaded world is required; the title screen is not

Test mode is available in a loaded single-player or multiplayer world, but not on the title screen or with no world.

- **Why:** the player needs a HUD and a stable game context; the title screen has no world indicator and adds no useful test scenario.
- **Alternative rejected:** allow it at the title. It complicates the no-capture rule and the HUD without helping the player.

### D3 — Level meter uses the existing RMS calculation

The capture callback feeds a test pipeline that computes RMS per frame using `ActivationDecision.CalculateRms`. The HUD shows a bounded level (for example, a normalized bar or dBFS value) and updates on each rendered frame.

- **Why:** the same measure already drives voice activity, so the meter reflects what the mod will actually see.
- **Alternative rejected:** peak-only meter. RMS is steadier and matches the activation decision.

### D4 — Monitoring is opt-in and uses the playback volume

`MicrophoneTestMonitor` defaults to `false`. When enabled, the captured frames are played through the normal output-device resolution path and scaled by the existing `PlaybackVolume` master, the same control that affects incoming voice.

- **Why:** this makes the test useful for validating the playback-volume fix without a second player, and it avoids a second volume concept that could disagree with normal voice.
- **Consequence:** the HUD and README warn that headphones are recommended; the player can lower `PlaybackVolume` before enabling monitoring if feedback is a concern.

### D5 — Settings and lifecycle

Add `MicrophoneTestKey` (default `F9`) and `MicrophoneTestMonitor` (default `false`) to `ModConfig`; include them in reset-to-defaults and the GMCM panel.
- If an existing config already binds `F9` to another PVC action, keep that action and move the test key to the next free function key; the panel shows the effective key.

- The active test state is a runtime field only.
- `ReturnedToTitle` and world unload stop the test, stop its streams and restore the normal state where applicable.
- The test key toggles the mode; the HUD shows `Mic test` and the level.
- A pure eligibility helper (`worldReady`, `not title`, `audio available`) is unit-tested.

### D6 — Tests

- Eligibility: no test at title/no world, test allowed in single-player and multiplayer loaded worlds.
- Meter: RMS of silence is zero and speech-like input raises it.
- Monitor: disabled produces no playback; enabled routes frames and follows `PlaybackVolume`; disabling stops it.
- Lifecycle: leaving the world stops the test; toggling off restores the previous microphone state.
- No packets: while test mode is active, the transport queue remains empty.

## Risks / Trade-offs

- **[Feedback when monitoring through speakers]** → Default monitoring off, monitor uses `PlaybackVolume`, HUD/README warning to lower it or use headphones before enabling.
- **[Test mode pauses voice chat during a session]** → Make the state visible and restore it on exit; document that the test is exclusive.
- **[Two audio streams contend on Linux]** → Stop normal streams before opening test streams; use the same device-resolution/fallback path.
- **[Microphone left open if the player forgets]** → Stop on title/world unload, never persist the active state, and show a persistent test indicator.
- **[Device disappears during a test]** → Reuse the existing fallback/error handling; stop the test with a clear log instead of crashing.

## Migration Plan

- Add two settings with defaults; existing config files remain valid.
- No data migration; test audio is never written.
- Rollback removes the test UI/settings; normal voice behavior is unchanged.

## Open Questions

None.
