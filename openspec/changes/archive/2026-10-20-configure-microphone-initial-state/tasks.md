# Tasks

## 1. Persisted start-microphone setting

- [x] 1.1 Add `StartMicrophoneEnabled` (bool, default `false`) to `Config/ModConfig.cs` with a documented XML comment; verify a serialization round-trip preserves the field and that the default is `false`
- [x] 1.2 Include `StartMicrophoneEnabled` in `ModConfig.ResetToDefaults` and leave it unchanged by `Normalize`; verify the defaults test asserts `false` after a reset
- [x] 1.3 Verify an existing `config.json` that omits the new field loads with `false` and does not report an error; add a unit test that deserializes JSON without the field
- [x] 1.4 Update the config round-trip test data so every persisted field, including the new boolean, is exercised in both directions

## 2. Initial microphone state on world load

- [x] 2.1 Add a pure `ShouldStartMicrophone(startMicrophoneEnabled, worldReady, multiplayer, audioAvailable)` decision helper; verify unit tests cover enabled/disabled, single-player, title screen and unavailable audio
- [x] 2.2 Use the helper in `ModEntry.OnSaveLoaded` after the session state is established: when it returns true, call `SetMicrophoneEnabled(true)` and let the existing open/fallback path run; verify the multiplayer-enabled case logs a started microphone and the single-player case never calls it
- [ ] 2.3 Keep the failure path soft: if the setting is true but capture cannot open, leave `_microphoneEnabled` false, preserve the setting for the next session and log the failure; verify with a forced invalid input device that the game keeps running and the indicator reports muted
- [x] 2.4 Verify the setting is never applied at the title screen or before a world is ready; add an assertion in the decision-helper tests and check the startup log shows no microphone open before `SaveLoaded`

## 3. Settings panel and session-toggle isolation

- [ ] 3.1 Add a second GMCM boolean for `StartMicrophoneEnabled`, separate from the existing runtime `Microphone enabled` control; its setter updates the config and saves it but does not call `SetMicrophoneEnabled`; verify the panel shows both controls with distinct labels and tooltips
- [x] 3.2 Update the runtime microphone control's tooltip to state that it is session-only and never changes the persisted start setting; verify the text matches the README
- [ ] 3.3 Update reset-to-defaults so it sets `StartMicrophoneEnabled = false` and leaves the current session microphone disabled; verify reset restores `false` in the written config
- [x] 3.4 Verify that `SetMicrophoneEnabled` (keybind `F7` and the runtime panel control) never assigns `StartMicrophoneEnabled` and never calls `SaveConfig`; toggle the microphone during a session and confirm the config file is unchanged

## 4. Documentation

- [x] 4.1 Add `StartMicrophoneEnabled` to the README settings table with its default and explain that it applies on the next multiplayer world load; verify the documented name/default match `ModConfig`
- [x] 4.2 Replace the README statement that the microphone is never persisted with the new behavior: start state comes from config, while the in-game toggle is session-only; verify the controls and privacy sections no longer contradict each other
- [x] 4.3 Document the migration behavior for existing config files (missing field means disabled) and the privacy implication of opting in; verify the wording matches the default behavior

## 5. Verification

- [x] 5.1 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all existing and new tests pass and the build has no warnings or errors
- [x] 5.2 Verify single-player end to end with `StartMicrophoneEnabled: true`: load a single-player save and confirm no microphone stream opens, no capture frames are counted and no indicator is drawn
- [x] 5.3 Verify a multiplayer world with `StartMicrophoneEnabled: false`: load the world and confirm the microphone starts muted, the indicator shows `Mic: muted`, `F7` enables it immediately and `F7` again mutes it
- [x] 5.4 Verify a multiplayer world with `StartMicrophoneEnabled: true`: load the world and confirm capture opens without pressing `F7`, the indicator shows `Mic: on`, and the first captured frames are counted
- [ ] 5.5 Verify the setting-enabled failure path with an unavailable input device: confirm the mod reports the failure, stays muted and leaves the game running
- [x] 5.6 Verify toggle isolation across a restart: toggle the microphone during a session, confirm `config.json` did not change, then reload a multiplayer world and confirm the original persisted start value is used again
- [ ] 5.7 Verify the GMCM panel when available: both microphone controls are present, the start option changes only the saved value, the runtime option changes only the session, and reset restores the documented defaults
- [x] 5.8 Verify the native packaging is unchanged: the build still deploys `Concentus.dll`, `PortAudioSharp.dll` and all five `lib/<rid>` PortAudio files
- [x] 5.9 Verify the setting works without GMCM: edit `StartMicrophoneEnabled` to `true` in `config.json`, load a multiplayer world with GMCM absent and confirm the microphone starts enabled from the file value

## Workflow follow-up

- The base change's `voice-capture` and `voice-chat-settings` specs were synced into `openspec/specs/` on 2026-10-20, so this change now has targets. If both changes are archived, archive `add-proximity-voice-chat` first.
- After the code and automated checks pass, hand the in-game manual checks from group 5 to the user for final confirmation.
- Archive this change once the user has confirmed the manual tests.
