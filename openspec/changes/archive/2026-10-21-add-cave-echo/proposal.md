# Proposal

## Why

Caves currently only reduce the voice volume and muffle it. That makes them quieter, but not recognisably different from standing behind a wall. A short echo/reverb tail would make mines and farm caves feel enclosed and immersive, which is the point of the environment preset.

## What Changes

- Replace the cave preset's “just quieter” character with a **short echo/reverb** applied to incoming voice while the listener is in a mine or farm cave.
- Keep a **mild** attenuation and low-pass so the dry voice stays intelligible; the echo becomes the defining cave effect instead of a heavy volume drop.
- Apply the echo to the mixed incoming voice, after per-speaker distance and obstacle gains, so it cannot make a distant or occluded speaker louder than the same speaker in the open.
- Keep the cave effect **local**: no voice packet, protocol or network change. It applies only to the listener's playback.
- Do not apply the cave echo to the local microphone test monitor; that monitor must remain a clean local reference.
- Keep the existing cave detection (`MineShaft`, `FarmCave`, `IslandFarmCave`) and the existing independence from the host's `WallOcclusion` setting.
- No new setting: the cave effect is automatic when the listener enters a cave.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `environment-acoustics`: the cave preset changes from “quieter and muffled” to “short echo/reverb plus mild attenuation”, with explicit rules for composition, locality and the microphone-test monitor.

## Impact

- **Code**: `Audio/EnvironmentAcoustics.cs`, a new echo/reverb processor, `Audio/PlaybackMixer.cs`, `ModEntry` cave-mode wiring, tests and `README.md`.
- **Audio**: one short delay line with feedback on the mixed output; the existing limiter and master volume stay in the signal path after the echo.
- **Settings**: no new setting and no config migration.
- **Network**: none; the effect is local to each listener.
- **Risks**: too much wet/feedback makes speech muddy; feedback must stay bounded; location changes must not leave a stale echo tail; the effect must not leak into the microphone test.
