# Tasks

## 1. Mixer gain path

- [x] 1.1 Change the per-speaker gain used by `UpdateSpeakerState` to distance/same-location only, removing the master volume from `ProximityMath.CalculateVolume`; verify the proximity math tests still cover full volume, silence at the radius and cross-location silence
- [x] 1.2 Add a master-volume field and `SetMasterVolume(float)` to `PlaybackMixer`; verify the value is clamped to `0..1` and is read without allocation or locking on the audio thread
- [x] 1.3 Apply the master volume after the peak-normalization stage in `PlaybackMixer.Mix`; verify a normalized frame is multiplied by the master and cannot exceed the output range

## 2. Regression tests

- [x] 2.1 Add a hot single-speaker test: compare output peaks at master `1.0` and `0.5` and assert the second is approximately half the first
- [x] 2.2 Add an overlapping-speakers test that hits the limiter, then assert lowering the master still lowers the output and master `0.0` produces silence
- [x] 2.3 Add an assertion that the mix never exceeds `[-1, 1]` before the master multiplication, preserving the no-clipping contract

## 3. Verification

- [x] 3.1 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [x] 3.2 In game, set playback to `100`, speak with one player and lower it to `50`; verify the voice becomes clearly quieter without restarting or changing streams
- [x] 3.3 In game with several overlapping speakers, lower the playback volume and verify the output still changes while remaining free of clipping

## Workflow follow-up

- Archive after the fix and the in-game checks are complete.
