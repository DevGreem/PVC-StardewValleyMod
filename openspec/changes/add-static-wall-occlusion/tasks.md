# Tasks

## 1. Static geometry and occlusion math

- [ ] 1.1 Add a pure occlusion helper with gain and low-pass alpha for 0, 1, 2 and 3+ blocked tiles; verify unit tests cover monotonic gain, the alpha floor and the silence threshold
- [ ] 1.2 Add a line-of-sight helper that walks the `Buildings` layer at 2× oversampling, skips endpoints and consecutive duplicate tiles, treats a true `Passable` value as an open door and a false/absent value as solid; verify tests with a fake grid cover a clear line, one wall, an open door and a diagonal wall
- [ ] 1.3 Handle a missing `Buildings` layer, out-of-bounds coordinates and non-blocking tiles safely; verify tests return zero blockers instead of throwing

## 2. Configuration, settings panel and documentation

- [ ] 2.1 Add `WallOcclusion` (boolean, default `true`) to `ModConfig`, `ResetToDefaults` and the config tests; verify old configs load it as `true` and reset restores it
- [ ] 2.2 Expose a "Wall occlusion" toggle in Generic Mod Config Menu with a tooltip explaining that it is local and applies to incoming voice; verify the panel lists it and reset restores `true`
- [ ] 2.3 Document the feature, the default, the one/two/three-wall behavior and the local-only scope in `README.md`; verify every documented value matches the code

## 3. Playback integration

- [ ] 3.1 Add per-channel low-pass target/state to `SpeakerChannel`, apply it in `TryReadFrame` with a short slew and reset it in `Clear`; verify tests show muffling, no filter when alpha is 1 and clean reset
- [ ] 3.2 Compose the distance gain with the occlusion gain and set the low-pass target in `UpdateSpeakerState` only for in-range speakers; verify composition tests show an occluded speaker is quieter at the same distance and silent when fully blocked
- [ ] 3.3 Verify the line-of-sight helper consults only the static `Buildings` layer, so trees, fences and furniture in other game collections cannot enter it; cover open versus closed doors with line-of-sight tests

## 4. Integration verification

- [ ] 4.1 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [ ] 4.2 In a two-player world, test a thin wall, a thick building wall and a closed versus open door; verify the voice becomes quieter, muffled or silent as expected
- [ ] 4.3 Toggle `WallOcclusion` in the panel during a session; verify the listener's audio changes immediately while the other player's playback is unaffected

## Workflow follow-up

- Archive after the two-player and toggle checks are complete.
- The future player-placed/dynamic block occlusion levels are tracked as a separate Super Productivity task; they are not part of this change.
