# Proposal

## Why

Voice currently ignores map geometry. Two farmers separated by a house wall hear each other exactly as if the wall were not there, which breaks the proximity illusion and lets players hear through buildings. Manual tests confirmed that static Stardew Valley walls do not block sound today.

## What Changes

- Add **static wall occlusion** to incoming voice: before a speaker is mixed, trace the line between the speaker and the listener over the current location's `Buildings` layer.
- Count the solid static tiles crossed by that line. A single wall makes the speaker quieter and muffled; two walls make the voice much quieter and heavily muffled; three or more walls make it silent. Open or passable doors do not count.
- Compose the occlusion gain with the existing distance falloff, so a speaker behind a wall is always quieter than the same speaker in the open at the same distance.
- Apply a short per-speaker low-pass filter so walking behind a wall sounds like muffling rather than an abrupt volume step.
- Add a persisted `WallOcclusion` setting, enabled by default, with a Generic Mod Config Menu toggle. It applies live and is local to each player.
- Compute occlusion on each client from its own map and the farmers' game-state positions. The packet format, sender position handling and wire-format version do not change.
- Explicitly leave **player-placed and dynamic blocks** (trees, fences, furniture and other objects) and per-material occlusion levels out of scope. That future work is recorded in Super Productivity as a separate task.

## Capabilities

### New Capabilities

- `wall-occlusion`: line-of-sight over static map geometry, which tiles count as blockers, and how blocked tiles attenuate, muffle or silence a speaker.

### Modified Capabilities

- `proximity-playback`: playback gain now combines distance falloff with the wall-occlusion result; fully occluded speakers are silent.
- `voice-chat-settings`: the settings panel exposes the `WallOcclusion` toggle, applies it live and restores it on reset.

## Impact

- **Code**: new static-geometry helper around the `Buildings` layer, a pure occlusion math helper, per-channel low-pass filtering in `SpeakerChannel`, occlusion composition in `ModEntry.UpdateSpeakerState`, `ModConfig`, `GmcmIntegration`, tests and `README.md`.
- **Settings**: one additive boolean `WallOcclusion` (default `true`); older config files load it as `true`.
- **Performance**: a short line walk (at most a few dozen tile samples per audible speaker per tick) plus one one-pole filter per speaking channel; no network cost.
- **Compatibility**: no wire-format change and no new packet field. Occlusion is local, so a player can disable it for their own playback without affecting others.
- **Risks**: decorative `Buildings` tiles may count as blockers; thick walls naturally count as multiple blockers; the MVP does not distinguish wall materials or dynamic objects.
