# Tasks

## 1. Static geometry and occlusion math

- [x] 1.1 Add a pure occlusion helper with gain and low-pass alpha for 0, 1, 2 and 3+ blocked tiles; verify unit tests cover monotonic gain, the alpha floor and the silence threshold
- [x] 1.2 Add a line-of-sight helper that walks the `Buildings` layer at 2× oversampling, skips endpoints and consecutive duplicate tiles, treats a true `Passable` value as an open door and a false/absent value as solid; verify tests with a fake grid cover a clear line, one wall, an open door and a diagonal wall
- [x] 1.3 Handle a missing `Buildings` layer, out-of-bounds coordinates and non-blocking tiles safely; verify tests return zero blockers instead of throwing
- [x] 1.4 Include constructed building footprints (cabins, sheds, barns, coops) from `GameLocation.buildings` in the line-of-sight count; verify a multi-tile cabin footprint reaches the occlusion threshold

## 2. Configuration, host synchronization and settings panel

- [x] 2.1 Add `WallOcclusion` (boolean, default `true`) to `ModConfig` and `ResetToDefaults` as the host's preference; verify old configs load it as `true` and reset restores it
- [x] 2.2 Add a `WallOcclusionMessage` and a pure sync policy that accepts only host messages, lets the host answer client requests and resolves clients to `true` until a host value arrives; verify tests cover host-only acceptance, request replies and the no-host fallback
- [x] 2.3 Broadcast the host value after a save loads, when a peer connects and when the host changes it; verify a late joiner receives the current value and a client cannot apply its own value
- [x] 2.4 Make the GMCM wall-occlusion control host-only: the host can change it, clients see the host's effective value and their changes are ignored; verify the tooltip and reset behavior
- [x] 2.5 Document the host-controlled behavior, the client lock and the host-without-mod fallback in `README.md`; verify every documented value matches the code

## 3. Playback integration

- [x] 3.1 Add per-channel low-pass target/state to `SpeakerChannel`, apply it in `TryReadFrame` with a short slew and reset it in `Clear`; verify tests show muffling, no filter when alpha is 1 and clean reset
- [x] 3.2 Use the host-controlled effective value in `UpdateSpeakerState` instead of the local config value; verify the host value reaches playback and clients fall back to `true` without a host
- [x] 3.3 Verify the line-of-sight helper consults the static `Buildings` layer and constructed building footprints, so trees, fences and furniture in other game collections cannot enter it; cover open versus closed doors and a cabin footprint with tests

## 4. Integration verification

- [x] 4.1 Add tests for the sync policy and run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [ ] 4.2 In a two-player world, test a thin wall, a thick building wall and a cabin/shed between the farmers; verify the voice becomes quieter, muffled or silent as expected
- [ ] 4.3 Verify the host can toggle wall occlusion and every client changes immediately, while a client cannot change the value
- [ ] 4.4 Start a session where the host does not run PVC; verify clients keep wall occlusion enabled and cannot change it locally

## Workflow follow-up

- Archive after the host/client and host-without-mod checks are complete.
- The future player-placed/dynamic block occlusion levels are tracked as a separate Super Productivity task; they are not part of this change.
