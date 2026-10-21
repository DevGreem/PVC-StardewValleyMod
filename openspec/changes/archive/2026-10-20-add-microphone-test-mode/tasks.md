# Tasks

## 1. Settings and configuration

- [x] 1.1 Add `MicrophoneTestKey` (`F9`) and `MicrophoneTestMonitor` (`false`) to `ModConfig` with documented XML comments; verify a serialization round-trip preserves both and that defaults match the README
- [x] 1.2 Include both settings in `ResetToDefaults` and `Normalize`; verify reset restores `F9` and `false`
- [x] 1.3 Expose the test keybind and monitor toggle in GMCM with distinct labels/tooltips; verify the panel lists them and that reset restores their defaults

## 2. Test-mode audio path

- [x] 2.1 Add a pure eligibility helper for test mode (`worldReady`, `not title`, `audioAvailable`); verify unit tests cover single-player, multiplayer, title/no world and unavailable audio
- [x] 2.2 Implement start/stop of test mode: stop normal voice capture/playback, open local capture and optional test playback, and restore the previous state when stopped; verify toggling off resumes the previous microphone state
- [x] 2.3 Route captured test samples to the level meter and, only when monitoring is enabled, to the selected output using `PlaybackVolume`; verify monitoring off produces no playback, monitoring on receives frames, and lowering `PlaybackVolume` lowers the monitor
- [x] 2.4 Ensure no voice packets are enqueued or sent while test mode is active; verify the transport queue/counters stay at zero during a test

## 3. UI and documentation

- [x] 3.1 Add a visible `Mic test` state and input-level meter to the HUD; verify it appears in single-player and multiplayer while the test is active and disappears when it stops
- [x] 3.2 Ensure the test HUD/control does not capture input; verify a click in the drawn area still reaches the game
- [x] 3.3 Document the test key, monitor default, headphone recommendation, single-player use and privacy/no-recording behavior in `README.md`; verify the documented defaults match `ModConfig`

## 4. Lifecycle and failure handling

- [x] 4.1 Stop test mode on `ReturnedToTitle`, world unload and game exit; verify the microphone and test playback streams close and the normal state is restored where possible
- [x] 4.2 Handle input-device open failure and device removal with the existing fallback/error path; verify the game keeps running, the test stops with a clear log and the HUD reflects the failure
- [x] 4.3 Verify the active test state is never written to `config.json`; toggle it and confirm the file only changes the two persisted test settings when the user changes them

## 5. Verification

- [x] 5.1 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [x] 5.2 In a single-player world enable the test, speak and verify the meter rises; toggle monitoring, verify you hear yourself only when it is enabled, and lower `PlaybackVolume` to confirm the monitor gets quieter
- [x] 5.3 In a multiplayer world enable the test and verify normal voice is paused, no packets are sent and disabling the test restores the previous microphone state
- [x] 5.4 Return to the title screen while the test is active; verify the microphone closes, monitoring stops and no capture continues
- [x] 5.5 Verify the panel and keybind work with GMCM absent by editing the settings file; confirm the test key and monitor setting are read from disk

## Workflow follow-up

- Archive after the in-game single-player, multiplayer and title-screen checks are complete.
- Stack this change after the microphone/radius changes if both are archived together; it modifies the `voice-capture` no-capture rule.
