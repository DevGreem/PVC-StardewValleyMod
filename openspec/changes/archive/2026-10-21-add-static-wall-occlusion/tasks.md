# Tasks

## 1. Static geometry and occlusion math

- [x] 1.1 Add a pure occlusion helper with gain and low-pass alpha for 0, 1, 2 and 3+ static obstacles; verify unit tests cover monotonic gain, the alpha floor and the silence threshold
- [x] 1.2 Add a line-of-sight helper that walks the `Buildings` layer at 2× oversampling, skips endpoints and consecutive duplicate tiles, counts a contiguous wall run as one obstacle, treats a true `Passable` value as an open door and a false/absent value as solid; verify tests with a fake grid cover a clear line, a thick wall, two separated walls, an open door and a diagonal wall
- [x] 1.3 Handle a missing `Buildings` layer, out-of-bounds coordinates and non-blocking tiles safely; verify tests return zero obstacles instead of throwing
- [x] 1.4 Include constructed building footprints (cabins, sheds, barns, coops) from `GameLocation.buildings` as one obstacle each; verify a multi-tile cabin yields one obstacle and remains audible instead of silent

## 2. Configuration, host synchronization and settings panel

- [x] 2.1 Add `WallOcclusion` (boolean, default `true`) to `ModConfig` and `ResetToDefaults` as the host's preference; verify old configs load it as `true` and reset restores it
- [x] 2.2 Add a `WallOcclusionMessage` and a pure sync policy that accepts only host messages, lets the host answer client requests and resolves clients to `true` until a host value arrives; verify tests cover host-only acceptance, request replies and the no-host fallback
- [x] 2.3 Broadcast the host value after a save loads, when a peer connects and when the host changes it; verify a late joiner receives the current value and a client cannot apply its own value
- [x] 2.4 Make the GMCM wall-occlusion control host-only: the host can change it, clients see the host's effective value and their changes are ignored; verify the tooltip and reset behavior
- [x] 2.5 Document the host-controlled behavior, the client lock and the host-without-mod fallback in `README.md`; verify every documented value matches the code

## 3. Playback integration

- [x] 3.1 Add per-channel low-pass target/state to `SpeakerChannel`, apply it in `TryReadFrame` with a short slew and reset it in `Clear`; verify tests show muffling, no filter when alpha is 1 and clean reset
- [x] 3.2 Use the host-controlled effective value in `UpdateSpeakerState` instead of the local config value; verify the host value reaches playback and clients fall back to `true` without a host
- [x] 3.3 Verify the line-of-sight helper consults the static `Buildings` layer and constructed building footprints, counting a thick wall or building as one obstacle, so trees, fences and furniture in other game collections cannot enter it; cover open versus closed doors and a cabin footprint with tests
- [x] 3.4 Add the cave environment preset for `MineShaft`, `FarmCave` and `IslandFarmCave`: quieter gain and a low-pass ceiling independent of `WallOcclusion`; verify tests cover the gain, the alpha cap and composition

## 4. Integration verification

- [x] 4.1 Add tests for the sync policy and run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [x] 4.2 In a two-player world, test a thin wall, a thick wall and a cabin/shed between the farmers; verify one obstacle is quieter and muffled, two are much quieter, and three or more are silent
- [x] 4.3 Verify the host can toggle wall occlusion and every client changes immediately, while a client cannot change the value

## Workflow follow-up

- Archive after the implementation and automated checks are complete.
- Post-release verification by players: cave sound and the host-without-PVC fallback in a real session.
- The future player-placed/dynamic block occlusion levels are tracked as a separate Super Productivity task; they are not part of this change.
