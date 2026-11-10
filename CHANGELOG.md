# Changelog

All notable changes to Proximity Voice Chat are documented here.

## 0.1.0 — 2026-11-10

First public release. Proximity Voice Chat brings in-game voice chat to Stardew Valley
multiplayer without Discord, bots, tokens or external voice services: install the mod and talk.

### Voice chat

- Proximity-based voice: volume falls off linearly with distance until the speaker's declared
  reach is reached.
- Selectable voice reach per player: 3, 15 or 30 tiles (`F8`). Each speaker's own reach decides
  how far others can hear them.
- Voice only plays between players in the same game location.
- Up to eight speakers mixed at once; the closest ones take priority.
- Two transmission modes: push-to-talk (hold `V`) or voice activity with an adjustable
  sensitivity threshold.
- 16 kbps Opus, 20 ms frames, small packets relayed through SMAPI, so no port forwarding, NAT
  traversal or dedicated server is needed.

### Spatial audio

- Static wall occlusion, controlled by the host and synced to every client: walls and closed
  doors quiet and muffle voice; three or more obstacles silence it. Open or passable doors do not
  block.
- Cave echo and mild attenuation in mines and farm caves, applied only to remote voice playback.

### Interface

- HUD indicator with microphone state, active reach, speaking players and locally muted players.
- Nearby player list with a "+N players nearby" overflow, plus an "All" view.
- Local player mute menu (`F10`). Mutes are per-player, stored in your config and never
  transmitted.
- Microphone test mode (`F9`), with optional local monitoring, usable in single-player. Test
  audio is never sent or recorded.
- Every action has a rebindable key, and all settings live in the in-game panel (Generic Mod
  Config Menu optional).

### Setup and compatibility

- Microphone starts disabled by default (`F7` to enable).
- Only players who want voice chat need the mod; anyone else keeps playing normally. The host
  needs SMAPI, but not this mod.
- Config files migrate automatically from older builds; unreadable files are backed up instead of
  overwritten.
- Native audio libraries bundled for Windows x64, Linux x64/arm64 and macOS Intel/Apple Silicon.
- Voice packets carry a wire-format version and are discarded if incompatible.
