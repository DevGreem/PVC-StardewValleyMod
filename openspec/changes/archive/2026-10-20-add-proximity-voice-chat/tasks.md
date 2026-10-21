# Tasks

## 1. Project scaffolding and build

- [x] 1.1 Create the mod project file targeting `net6.0` with `Pathoschild.Stardew.ModBuildConfig`, `PortAudioSharp2` and `Concentus`; verify `dotnet build` succeeds and produces the mod DLL
- [x] 1.2 Add `manifest.json` (name, unique ID, author, version, minimum SMAPI version, entry DLL, no required dependencies); verify SMAPI loads the mod and prints it in the console without an error
- [ ] 1.3 Create the mod entry point and wire the lifecycle hooks (game launched, save loaded, returned to title, update tick, button pressed/released, mod message received); verify each hook logs once on its event during a normal session
- [x] 1.4 Add a unit test project for the logic that needs no audio hardware (packet framing, validation, distance falloff, activation decision); verify `dotnet test` runs and passes with the placeholder tests

## 2. Settings

- [x] 2.1 Implement the config model with documented defaults, the fixed proximity-radius set, activation mode, keybinds and device names; verify a serialization round-trip test preserves every field
- [x] 2.2 Load and save the config, falling back to defaults when the file is missing or unreadable and reporting the problem; verify with a deliberately corrupted config file that the mod starts and logs the fallback
- [x] 2.3 Apply changes to playback volume, radius, activation mode and devices live; verify each takes effect without a restart, and that only a device change restarts the audio streams
- [ ] 2.4 Register the Generic Mod Config Menu panel exposing every setting plus reset-to-defaults; verify the panel opens in-game, lists each setting, and that reset restores the documented defaults
- [x] 2.5 Verify the mod starts and runs with Generic Mod Config Menu absent, using the saved config values
- [x] 2.6 Document every setting and its default in `README.md`; verify each documented option is present in the panel and matches its default

## 3. Native audio bootstrap

- [x] 3.1 Implement per-platform native library resolution that maps the requested library name to the correct file name inside the mod folder for Windows, macOS and Linux, with a log line naming the file that was loaded; verify the log reports the expected file on each platform
- [x] 3.2 Initialize the audio backend, enumerate input and output devices, open and close capture and playback streams, and fail soft when initialization is impossible; verify device enumeration lists the machine's devices and that a forced initialization failure disables voice with a clear error instead of crashing the game

## 4. Voice codec

- [x] 4.1 Wrap the Opus encoder and decoder at the chosen sample rate, channel count, frame duration and bitrate, with packet-loss resilience enabled; verify a round-trip test (encode a known signal, decode it, assert frame length and non-silence)
- [x] 4.2 Verify the encoded packet size stays within the wire cap across varied input (silence, speech-like noise, loud noise)
- [x] 4.3 Handle encode and decode failures by dropping the affected frame; verify a forced failure is discarded without throwing and without stopping the pipeline

## 5. Microphone capture

- [x] 5.1 Accumulate captured audio into fixed-duration frames; verify a counter shows frames arriving at the expected cadence for a second
- [x] 5.2 Implement both activation modes — push-to-talk while the key is held, and voice activity above the configured sensitivity threshold; verify unit tests on the activation decision plus a manual check that each mode transmits only when it should
- [x] 5.3 Start each session with the microphone disabled, and toggle it from the keybind and the settings panel; verify the state flips on the next frame with no restart and that no audio leaves the machine while disabled
- [x] 5.4 Suppress capture outside a loaded multiplayer world and in single-player; verify nothing is captured or transmitted when playing alone or on the title screen
- [x] 5.5 Document the controls, the activation modes and the default keybinds in `README.md`; verify the documented keys match the panel defaults

## 6. Voice transport

- [x] 6.1 Implement the versioned, length-prefixed packet model with caps on the audio payload and the location name; verify unit tests cover a successful round-trip and rejection of malformed, oversized, empty-location and over-long-location packets
- [x] 6.2 Reject packets whose wire-format version the mod does not support; verify a packet with a bumped version is discarded and produces no audio
- [x] 6.3 Implement the send path, addressing only the players of the current session; verify own packets echoed back and mod messages from other mods are both ignored
- [x] 6.4 Implement receive-side structural validation, per-sender rate limiting and validation of the declared radius against the offered set; verify unit tests accept normal speech rates, drop a flood from one sender, and reject an unsupported radius
- [x] 6.5 Derive the speaker's position from the game's farmer state rather than from the packet; verify a hand-crafted packet claiming a different position does not change the position used for proximity
- [ ] 6.6 Release a peer's buffered audio and rate-limit state on disconnect; verify the state is gone immediately after a peer leaves, without waiting for a timeout
- [x] 6.7 Document the version-compatibility rule in `README.md`; verify the documented rule matches the wire-format version constant in the code

## 7. Proximity playback

- [x] 7.1 Implement one channel per speaker with its own decoder and jitter buffer, concealing a lost frame; verify that dropping one packet produces concealed audio rather than a gap
- [x] 7.2 Gate playback on the speaker sharing the local player's location; verify no audio is played across locations and that a speaker walking out mid-sentence goes silent
- [x] 7.3 Implement distance-based volume falloff reaching silence at the radius, scaled by the master volume; verify unit tests assert full volume at zero distance, silence at the radius, monotonic decrease between them, and silence when the master volume is zero
- [x] 7.4 Mix concurrent speakers with a bound on how many are mixed at once and keep the result from clipping; verify that many loud overlapping voices stay within range and that the closest speakers win when over the bound
- [x] 7.5 Make playback pull mixed frames from the audio thread so it does not depend on the game loop; verify voice keeps playing while the game is deliberately hitched (autosave or a heavy tick)
- [x] 7.6 Continue playback when a frame cannot be decoded; verify a forced decode failure is skipped and the speaker's next frames still play
- [x] 7.7 Select the output device with a fallback to the system default; verify that removing the selected device mid-session falls back and reports the substitution

## 8. On-screen indicator

- [x] 8.1 Show the local microphone state (transmitting or muted); verify the indicator changes when the microphone is toggled
- [x] 8.2 List nearby players who are currently speaking, limited to the local player's location and the active radius; verify a speaker who walks out of range disappears and a silent in-range player is not shown as speaking
- [x] 8.3 Display the active proximity radius; verify it updates immediately when the radius is cycled
- [x] 8.4 Hide the indicator in single-player and when the player turns it off in settings; verify neither case draws anything
- [x] 8.5 Ensure the indicator does not capture input; verify a click in the area where it is drawn still reaches the game
- [x] 8.6 Document the indicator and how to hide it in `README.md`; verify the documented setting exists and behaves as described

## 9. Packaging and integration verification

- [x] 9.1 Package the native audio libraries for every supported platform so the built mod folder contains the correct file for each; verify the contents of the build output for Windows, macOS and Linux
- [ ] 9.2 Run a two-player session end to end; verify voice is heard, volume drops as the speaker walks away, and no voice crosses between game locations
- [x] 9.3 Verify single-player has no effect: no microphone is opened and nothing is transmitted
- [ ] 9.4 Verify a player without the mod is inaudible and hears nothing, and that the README states this
- [ ] 9.5 Verify a full session with Generic Mod Config Menu absent still works end to end
- [ ] 9.6 Smoke-test the built mod on each supported platform; verify it loads, enumerates devices and carries voice on Windows, macOS and Linux

## Workflow follow-up

- Archive the change once implementation and the project's review requirements are satisfied.
- Verify the archived result: the capability specs appear under `openspec/specs/` and the change is under `openspec/changes/archive/`.
- Keep the Super Productivity project *Proximity Voice Chat* in sync as roadmap items (panning, occlusion, whisper, per-player mute, noise suppression, sidetone, UDP transport) are picked up.
