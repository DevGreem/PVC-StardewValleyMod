# Tasks

## 1. Radius set and configuration

- [x] 1.1 Reduce `ModConstants.ProximityRadii` to `{3, 10, 20}`, keep `DefaultProximityRadius = 10`, and update `NextRadius`/config tests; verify cycling goes `3 → 10 → 20 → 3`
- [x] 1.2 Verify `ModConfig.Normalize` maps old `6`/`15` values to `10`, keeps `3/10/20`, and logs the normalization at info/trace level without reporting an error; add a unit test using a legacy config value
- [x] 1.3 Update the config test data and README documentation for the reduced set and the outgoing-reach semantics

## 2. Receiver uses the speaker's declared radius

- [x] 2.1 Store the validated packet radius on `SpeakerChannel` when a packet is accepted; verify two speakers with different radii keep independent values
- [x] 2.2 Use the channel radius for the same-location/distance gate and volume falloff in `UpdateSpeakerState`; verify the local `ProximityRadius` no longer changes what the local player hears
- [x] 2.3 Add unit tests for packet-radius gating: in-range at `3` is audible, a listener with a smaller local radius still hears a speaker declaring `10`, and a speaker at/over its declared radius is silent
- [x] 2.4 Add an indicator test proving nearby speakers are filtered by their declared radius and the displayed radius is the local outgoing reach

## 3. UI and documentation

- [x] 3.1 Relabel the GMCM option and HUD line as the local voice reach, with a tooltip explaining it does not filter incoming voice; verify the text matches the README
- [x] 3.2 Document the privacy model, the three values, the default and the `6`/`15` migration in `README.md`; verify each documented value matches the code
- [x] 3.3 Update the indicator tests for the outgoing-radius display and per-speaker nearby filtering

## 4. Verification

- [x] 4.1 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [x] 4.2 In a two-player world set the speaker's radius to `3`, then `10`, then `20`; verify the listener stops hearing the speaker at the corresponding distance without changing the listener's own setting
- [x] 4.3 Verify the listener's own radius does not silence a speaker who declares a larger radius, and that the HUD nearby list follows the speaker's declared radius
- [x] 4.4 Load a legacy config containing `6` or `15`; verify it normalizes to `10`, and verify `20` remains valid, without errors

## Workflow follow-up

- Archive after the two-player checks and the config-migration check are complete.
- `fix-playback-volume` is already archived; `UpdateSpeakerState` uses distance-only channel gain, so this change only swaps the radius source from the local config to the speaker's declared value.
