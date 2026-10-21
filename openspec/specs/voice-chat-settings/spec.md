# voice-chat-settings Specification

## Purpose

Holds the mod's configuration and exposes it to the player, so that devices, volume, range, activation mode and keybinds can be changed in-game without editing files or restarting.

## Requirements

### Requirement: Settings persist between sessions

The mod SHALL store its settings on disk and reload them the next time the game starts.

#### Scenario: First run

- **WHEN** the mod runs for the first time
- **THEN** a settings file is created with documented default values

#### Scenario: Later run

- **WHEN** the player has changed settings and starts the game again
- **THEN** the saved settings are applied

### Requirement: Unreadable settings fall back to defaults

The mod SHALL fall back to default values when its settings file is missing, unreadable or invalid, and SHALL report the problem instead of failing to load.

#### Scenario: Corrupted settings file

- **WHEN** the settings file cannot be parsed
- **THEN** the mod starts with default settings and reports the problem

### Requirement: In-game settings panel

When Generic Mod Config Menu is installed, the mod SHALL register a settings panel exposing every supported audio, voice, keybind, microphone-test, player-mute and wall-occlusion setting, plus the current session microphone and test controls. The panel SHALL explain that player mutes are local and where their menu opens. The wall-occlusion control SHALL be read-only for clients.

#### Scenario: Panel is available

- **WHEN** Generic Mod Config Menu is installed
- **THEN** the player can view and change every supported setting, sees the player-mute explanation and can use the session microphone and test controls

#### Scenario: Client opens the panel

- **WHEN** a non-host player opens the wall-occlusion control
- **THEN** the control shows the host's effective value and does not allow the client to change it

### Requirement: Settings panel is optional

The mod SHALL run with its saved and default settings when Generic Mod Config Menu is not installed.

#### Scenario: Generic Mod Config Menu is missing

- **WHEN** Generic Mod Config Menu is not installed
- **THEN** the mod loads and runs using the values in its settings file

### Requirement: Settings changes apply without a restart

The mod SHALL apply playback volume, proximity radius, activation mode, audio device, microphone test, player-mute key and local-monitor changes immediately without a restart or world reload. Mute changes SHALL apply to local playback immediately. Host wall-occlusion changes SHALL apply to every player; client attempts SHALL be ignored.

#### Scenario: Player changes the input device

- **WHEN** the player selects a different microphone in the panel
- **THEN** capture moves to that device immediately

#### Scenario: Player changes the proximity radius

- **WHEN** the player changes the proximity radius
- **THEN** the local player's outgoing voice reach changes immediately for subsequent packets

#### Scenario: Player changes the microphone test settings

- **WHEN** the player changes the test-monitor toggle
- **THEN** the change applies to the current or next test without restarting the game

#### Scenario: Host toggles wall occlusion

- **WHEN** the host enables or disables wall occlusion during a session
- **THEN** every client's incoming voice reflects the new occlusion behavior immediately

#### Scenario: Client tries to toggle wall occlusion

- **WHEN** a client tries to enable or disable wall occlusion during a session
- **THEN** the client keeps the host's value and its local change is ignored

#### Scenario: Player mutes someone from the menu

- **WHEN** the player toggles a player's mute in the player menu
- **THEN** that player is silenced or restored locally on the next frame

### Requirement: Start-microphone changes apply on next load

The start-microphone setting SHALL be read only when initializing a multiplayer world and SHALL NOT change the current session's microphone state.

#### Scenario: Player changes the start-microphone setting

- **WHEN** the player changes the persisted start-microphone setting during a session
- **THEN** the current session microphone state is unchanged and the new value is used the next time a multiplayer world loads

### Requirement: Proximity radius is chosen from a fixed set

The mod SHALL offer exactly the proximity radii `3`, `15` and `30` tiles, SHALL use `15` as the documented default, and SHALL NOT accept an arbitrary value. On load, `3` and `15` remain unchanged, while legacy `6`, `10` and `20` normalize to `3`, `15` and `30` respectively; any other value normalizes to the default.

#### Scenario: Player cycles the radius

- **WHEN** the player cycles the proximity radius with the configured key
- **THEN** the radius moves through `3`, `15`, `30` and back to `3`

#### Scenario: Old configuration contains a removed radius

- **WHEN** the settings file contains `6`, `10` or `20` from an older build
- **THEN** the loaded radius becomes `3`, `15` or `30` respectively and no error is reported

#### Scenario: Old configuration already uses a valid legacy value

- **WHEN** the settings file contains `3` or `15`
- **THEN** the loaded radius is preserved

#### Scenario: Configuration contains an arbitrary value

- **WHEN** the settings file contains a radius outside the current and legacy sets
- **THEN** the loaded radius becomes the default `15` and no error is reported

### Requirement: Microphone toggle keybind

The mod SHALL provide a keybind that toggles the microphone during play. The toggle SHALL change only the current session state, SHALL update the on-screen indicator immediately and SHALL NOT modify the persisted start-microphone setting or write it to disk.

#### Scenario: Toggling the microphone

- **WHEN** the player presses the microphone keybind
- **THEN** the current session microphone state flips, the indicator reflects the new state and the persisted start-microphone setting remains unchanged

### Requirement: Settings can be reset to defaults

The mod SHALL provide a way to restore every setting, including the start-microphone setting, microphone test key, player-mute menu key, local-monitor toggle and wall-occlusion toggle, to its documented default value. Resetting SHALL restore the host's wall-occlusion preference to enabled and broadcast it; a client's reset SHALL NOT override the host's value. Resetting SHALL clear the local mute list. Resetting SHALL also leave the current session microphone and microphone test mode disabled.

#### Scenario: Player resets the settings

- **WHEN** the player resets the settings
- **THEN** every setting returns to its documented default, the local mute list is cleared, the start-microphone setting becomes disabled, the current session microphone and test mode are disabled and wall occlusion returns to the host's default

#### Scenario: Host resets wall occlusion

- **WHEN** the host resets the settings
- **THEN** wall occlusion becomes enabled and every client applies that value

### Requirement: Keybinds are player-configurable

The mod SHALL let the player choose the push-to-talk key, the proximity cycle key, the microphone toggle key, the microphone test key and the player-mute menu key, and SHALL accept any key the game itself can bind.

#### Scenario: Player rebinds push-to-talk

- **WHEN** the player assigns a different push-to-talk key
- **THEN** the new key takes effect immediately and the old one no longer transmits

#### Scenario: Player rebinds the microphone test key

- **WHEN** the player assigns a different microphone test key
- **THEN** the new key starts or stops the test immediately and the old key no longer does

#### Scenario: Player rebinds the player-mute menu key

- **WHEN** the player assigns a different player-mute menu key
- **THEN** the new key opens the menu immediately and the old key no longer does

### Requirement: Microphone start state persists

The mod SHALL store a start-microphone boolean in its settings file and SHALL read it only when initializing a multiplayer world. The default value SHALL be disabled so a first run never opens the microphone without an explicit player choice.

#### Scenario: First run

- **WHEN** the mod runs for the first time
- **THEN** the settings file is created with the start-microphone setting disabled

#### Scenario: Later multiplayer session

- **WHEN** the start-microphone setting is enabled and a new multiplayer world loads
- **THEN** the session microphone starts enabled if the input device can be opened

### Requirement: Keybinds are inactive while typing

The mod SHALL ignore its gameplay keybinds and SHALL NOT transmit push-to-talk while the chat box is active.

#### Scenario: Player types the push-to-talk key in chat

- **WHEN** the chat box is active and the player presses the push-to-talk key
- **THEN** the microphone does not transmit and the character reaches the chat box

#### Scenario: Player presses another PVC keybind in chat

- **WHEN** the chat box is active and the player presses a PVC microphone, test, radius or player-mute keybind
- **THEN** that action does not fire and the key is left to the chat input

#### Scenario: Player closes chat

- **WHEN** the chat box is closed after typing
- **THEN** the PVC keybinds work normally again
