# Proposal

## Why

The mod currently forces the microphone off every time a multiplayer save loads, so a player who always wants voice chat on must press the toggle again after every session. The desired behavior is to treat the persisted setting as the initial microphone state when entering a multiplayer game, while keeping in-session toggles temporary so a session change never rewrites the preference.

## What Changes

- Add a persisted `StartMicrophoneEnabled` boolean to the mod settings, defaulting to `false` to preserve the current privacy-safe behavior.
- When a multiplayer save finishes loading, initialize the session microphone state from `StartMicrophoneEnabled`: `true` opens the microphone and starts capture, `false` leaves it muted.
- If the setting is `true` but the microphone cannot be opened, keep the session muted, report the failure and leave the game running.
- Keep the in-game microphone toggle (`F7` and the settings panel) as a session-only action: it changes the live microphone state but does not modify `StartMicrophoneEnabled` or write it to disk.
- Expose `StartMicrophoneEnabled` in Generic Mod Config Menu as a separate option from the runtime "Microphone enabled" control; changing it affects the next multiplayer session, not the current one.
- Keep single-player unchanged: the microphone is never opened and the new setting has no effect.
- Reset-to-defaults sets `StartMicrophoneEnabled` to `false` and leaves the current session muted.
- Update the README and tests to describe and verify the initial-state behavior.
- **BREAKING** (behavioral, not API): a player who sets `StartMicrophoneEnabled` to `true` will begin transmitting as soon as a multiplayer world loads, rather than starting muted. The setting is off by default and the on-screen indicator still shows the state.

This change stacks on the in-progress `add-proximity-voice-chat` change; the `voice-capture` and `voice-chat-settings` capabilities are defined there and are modified here.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `voice-capture`: replaces the "always starts disabled" session rule with an initial state taken from configuration, while preserving the single-player and runtime-toggle rules.
- `voice-chat-settings`: adds the persisted `StartMicrophoneEnabled` setting, clarifies that the runtime microphone toggle does not save configuration, and updates the panel and reset requirements.

## Impact

- **Code**: `Config/ModConfig.cs`, `ModEntry.cs`, `Integrations/GmcmIntegration.cs`, the unit tests and `README.md`.
- **Settings file**: one new boolean field; existing config files remain valid and receive `false` by default.
- **Privacy**: the default remains muted, but explicitly opting in now persists across sessions. The indicator must continue to make the state visible.
- **Audio/transport**: no wire-format, codec, packet or networking changes. The microphone is opened on save load only when both the setting and the multiplayer-world conditions allow it.
- **Compatibility**: no migration is required; the new field is additive and defaults to the current behavior.
