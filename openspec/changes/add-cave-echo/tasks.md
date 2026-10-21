# Tasks

## 1. Cave echo processor

- [x] 1.1 Add a `CaveEcho` processor with a preallocated delay buffer, bounded feedback, wet mix and a soft feedback low-pass; verify tests cover the delayed impulse, feedback decay, passthrough when disabled, reset and finite output
- [x] 1.2 Change `EnvironmentAcoustics` cave values to a mild gain (`0.80`) and low-pass (`0.55`) so the echo is the main cave cue; verify tests cover the new gain and alpha ceiling

## 2. Mixer and game-thread integration

- [x] 2.1 Add `PlaybackMixer.SetCaveMode(bool)` and run the echo on the summed mix before the limiter and master volume; verify tests show a delayed tail in cave mode, no echo when disabled and master-volume scaling after the echo
- [x] 2.2 Set cave mode from `EnvironmentAcoustics.IsCave` in `UpdateSpeakerState` and force it off while the microphone test monitor is the source; verify the test-monitor test/behavior shows no echo and that the next speaker tick restores cave mode

## 3. Documentation

- [x] 3.1 Update `README.md` to describe the cave echo, the mild attenuation and the fact that the microphone test monitor stays clean; verify every documented behavior matches the code

## 4. Integration verification

- [x] 4.1 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors

## Workflow follow-up

- Post-release verification by players: cave echo intelligibility, no echo outdoors, and a clean microphone test monitor.
- Archive after the automated checks are complete.
- The existing host-controlled wall occlusion and dynamic-block roadmap work are separate changes.
