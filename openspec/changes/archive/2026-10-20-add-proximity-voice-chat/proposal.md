# Proposal

## Why

The predecessor mod (LVC) never touches the game's microphone: it delegates all audio to Discord, so every player must create a bot, a server and a set of permissions before a single word can be spoken. Voice is also grouped by game zone rather than by real distance, so two farmers standing side by side in a big area sound exactly like two farmers on opposite edges of it.

This change starts a new mod that captures the player's microphone and plays other players' voices back inside Stardew Valley itself, with volume driven by how far away each speaker is. No Discord, no bot token, no per-zone channels: install the mod and talk.

## What Changes

- Create a new SMAPI mod in this repository (`Proximity Voice Chat`) built from scratch — this repository is currently empty apart from scaffolding.
- Capture the local microphone in-game and encode it to Opus frames.
- Transmit voice frames to the other players of the session through the game's own multiplayer network, with the sender's position taken from game state rather than from the packet.
- Decode and mix incoming voice on the listener's machine, with volume falling off as the speaker gets further away.
- Only players in the same game location can hear each other; distance is measured in tiles within that location.
- Two microphone activation modes: push-to-talk (hold a key) and voice activity (configurable sensitivity threshold).
- In-game settings panel via Generic Mod Config Menu: input device, output device, master playback volume, proximity radius, activation mode and keybinds.
- On-screen indicator showing which nearby players are currently speaking.
- **BREAKING** (relative to LVC): the entire Discord layer is dropped — bot token, guild ID, voice channels per zone, category management and the Discord.Net dependency all go away. There is no migration path for existing LVC configuration; PVC ships its own config file.
- Cross-platform: Windows, macOS and Linux, matching SMAPI's own platform support.
- Out of scope for this change (tracked as future work in the Super Productivity project *Proximity Voice Chat*): stereo panning, wall occlusion, whisper key, per-player mute, noise suppression, sidetone and a low-latency UDP transport.

## Capabilities

### New Capabilities

- `voice-capture`: turning the local microphone into encoded voice frames — device selection, frame sizing, push-to-talk and voice-activity activation, and the decision to send or stay silent.
- `voice-transport`: carrying voice frames between players over the game's multiplayer network — packet framing, sending, receiving, structural validation, per-sender rate limiting and deriving the speaker's position from game state.
- `proximity-playback`: turning received voice frames back into audio — jitter buffering, same-location gating, distance-based volume falloff, mixing of concurrent speakers and output device selection.
- `voice-chat-settings`: the persisted configuration and its in-game panel — devices, master volume, proximity radius, activation mode and keybinds, applied without restarting the game.
- `voice-activity-indicator`: the on-screen feedback showing which nearby players are currently speaking.

### Modified Capabilities

None. This repository has no existing specs.

## Impact

- **New code**: the whole mod. No existing source in this repository to modify; `LVC-StardewValleyMod` stays untouched as a separate project.
- **Runtime dependencies**: `Pathoschild.Stardew.ModBuildConfig` (build), `PortAudioSharp2` (cross-platform capture/playback), `Concentus` (pure-managed Opus), and Generic Mod Config Menu (soft dependency, resolved at runtime).
- **Native binaries**: PortAudio needs a native library per platform, shipped alongside the mod. This is the main cross-platform risk and is addressed in `design.md`.
- **Target framework**: `net6.0`, matching Stardew Valley 1.6 / SMAPI 4.x.
- **Player-facing**: all players in a session must run the same major version of the mod; a player without it simply cannot be heard and hears nobody. Voice chat has no effect in single-player.
- **Privacy**: the mod opens the microphone and sends audio to the other players of the session. It must be opt-in per session (microphone starts muted or unconfigured until the player picks a device), and the indicator must make it obvious when the player is transmitting.
