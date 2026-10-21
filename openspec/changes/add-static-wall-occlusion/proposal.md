# Proposal

## Why

Voice currently ignores map geometry. Two farmers separated by a house wall hear each other exactly as if the wall were not there, which breaks the proximity illusion and lets players hear through buildings. Manual tests confirmed that static Stardew Valley walls do not block sound today.

## What Changes

- Add **static wall occlusion** to incoming voice: before a speaker is mixed, trace the line between the speaker and the listener over the current location's `Buildings` layer and the footprints of constructed buildings (cabins, sheds, barns, coops).
- Count **obstacles** crossed by that line, not every tile in a thick wall: one contiguous run of solid map tiles counts as one obstacle, and each constructed building (cabin, shed, barn, coop) counts as one obstacle.
- One obstacle makes the speaker quieter and muffled; two make the voice much quieter and heavily muffled; three or more make it silent. This keeps a single cabin wall audible but clearly occluded instead of cutting the voice completely. Open or passable doors on the map layer do not count.
- Compose the occlusion gain with the existing distance falloff, so a speaker behind a wall is always quieter than the same speaker in the open at the same distance.
- Apply a short per-speaker low-pass filter so walking behind a wall sounds like muffling rather than an abrupt volume step.
- Add a **cave environment preset**: in mines and farm caves, incoming voice is quieter and more muffled, independently of the host's wall-occlusion setting.
- Add a persisted `WallOcclusion` setting, enabled by default. The **host** is the only player who can change it; clients follow the host's value and cannot override it locally. If the host does not run PVC, the setting stays enabled for every client.
- Synchronize the host's value with a small SMAPI mod message when the host loads a save, when a peer connects and whenever the host changes it. Only messages that come from the host are accepted.
- Compute occlusion on each client from its own map and the farmers' game-state positions. The packet format, sender position handling and wire-format version do not change.
- Explicitly leave **player-placed and dynamic blocks** (trees, fences, furniture and other objects) and per-material occlusion levels out of scope. That future work is recorded in Super Productivity as a separate task.

## Capabilities

### New Capabilities

- `wall-occlusion`: line-of-sight over static map geometry and constructed building footprints, how obstacles attenuate, muffle or silence a speaker, and the host-controlled on/off setting.
- `environment-acoustics`: location-based acoustic presets, starting with a quieter and more muffled cave environment.

### Modified Capabilities

- `proximity-playback`: playback gain now combines distance falloff with the wall-occlusion result; fully occluded speakers are silent.
- `voice-chat-settings`: the settings panel exposes the `WallOcclusion` toggle, applies it live and restores it on reset.

## Impact

- **Code**: new static-geometry helper around the `Buildings` layer and `GameLocation.buildings` footprints, a pure occlusion math helper, a cave environment preset, per-channel low-pass filtering in `SpeakerChannel`, occlusion composition in `ModEntry.UpdateSpeakerState`, `ModConfig`, `GmcmIntegration`, tests and `README.md`.
- **Settings**: one additive boolean `WallOcclusion` (default `true`), persisted as the host's preference. Clients ignore their local value during a session and use the host's value.
- **Performance**: a short line walk (at most a few dozen tile samples per audible speaker per tick) plus one one-pole filter per speaking channel; a tiny settings message on host changes and peer connects.
- **Compatibility**: no voice wire-format change and no new voice packet field. A separate SMAPI settings message carries the host boolean. Occlusion itself is still computed locally by each client.
- **Risks**: decorative `Buildings` tiles may count as blockers; thick walls naturally count as multiple blockers; the MVP does not distinguish wall materials or dynamic objects. A client whose host does not run PVC keeps occlusion enabled and cannot change it.
