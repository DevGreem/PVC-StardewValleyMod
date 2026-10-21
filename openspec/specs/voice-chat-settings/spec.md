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

When Generic Mod Config Menu is installed, the mod SHALL register a settings panel exposing the input device, output device, playback volume, proximity radius (presented as the local voice reach), activation mode, the persisted start-microphone setting, push-to-talk key, microphone toggle key, radius cycle key, microphone test key and local-monitor toggle. It SHALL expose the current session microphone state and the microphone test toggle as separate runtime controls that do not modify the persisted start-microphone setting.

#### Scenario: Panel is available

- **WHEN** Generic Mod Config Menu is installed
- **THEN** the player can view and change every setting from the in-game panel, including the start-microphone setting, the current session microphone state and the microphone test controls

### Requirement: Settings panel is optional

The mod SHALL run with its saved and default settings when Generic Mod Config Menu is not installed.

#### Scenario: Generic Mod Config Menu is missing

- **WHEN** Generic Mod Config Menu is not installed
- **THEN** the mod loads and runs using the values in its settings file

### Requirement: Settings changes apply without a restart

The mod SHALL apply changes to playback volume, proximity radius, activation mode, audio devices, microphone test key and local-monitor toggle immediately, without requiring a game restart or a world reload. Changing the proximity radius SHALL change the local player's outgoing voice reach immediately. Changing the start-microphone setting SHALL take effect on the next multiplayer world load and SHALL NOT change the microphone state of the current session.

#### Scenario: Player changes the input device

- **WHEN** the player selects a different microphone in the panel
- **THEN** capture moves to that device immediately

#### Scenario: Player changes the proximity radius

- **WHEN** the player changes the proximity radius
- **THEN** the local player's outgoing voice reach changes immediately for subsequent packets

#### Scenario: Player changes the microphone test settings

- **WHEN** the player changes the test-monitor toggle
- **THEN** the change applies to the current or next test without restarting the game

#### Scenario: Player changes the start-microphone setting

- **WHEN** the player changes the persisted start-microphone setting during a session
- **THEN** the current session microphone state is unchanged and the new value is used the next time a multiplayer world loads

### Requirement: Proximity radius is chosen from a fixed set

The mod SHALL offer exactly the proximity radii `3`, `10` and `20` tiles, SHALL use `10` as the documented default, and SHALL NOT accept an arbitrary value. A loaded value outside the offered set SHALL be normalized to the default.

#### Scenario: Player cycles the radius

- **WHEN** the player cycles the proximity radius with the configured key
- **THEN** the radius moves to the next offered value

#### Scenario: Old configuration contains a removed radius

- **WHEN** the settings file contains `6` or `15` from an older build
- **THEN** the loaded radius is normalized to the default `10` and no error is reported

### Requirement: Microphone toggle keybind

The mod SHALL provide a keybind that toggles the microphone during play. The toggle SHALL change only the current session state, SHALL update the on-screen indicator immediately and SHALL NOT modify the persisted start-microphone setting or write it to disk.

#### Scenario: Toggling the microphone

- **WHEN** the player presses the microphone keybind
- **THEN** the current session microphone state flips, the indicator reflects the new state and the persisted start-microphone setting remains unchanged

### Requirement: Settings can be reset to defaults

The mod SHALL provide a way to restore every setting, including the start-microphone setting, microphone test key and local-monitor toggle, to its documented default value. Resetting SHALL also leave the current session microphone and microphone test mode disabled.

#### Scenario: Player resets the settings

- **WHEN** the player resets the settings
- **THEN** every setting returns to its documented default, the start-microphone setting becomes disabled and the current session microphone and test mode are disabled

### Requirement: Keybinds are player-configurable

The mod SHALL let the player choose the push-to-talk key, the proximity cycle key, the microphone toggle key and the microphone test key, and SHALL accept any key the game itself can bind.

#### Scenario: Player rebinds push-to-talk

- **WHEN** the player assigns a different push-to-talk key
- **THEN** the new key takes effect immediately and the old one no longer transmits

#### Scenario: Player rebinds the microphone test key

- **WHEN** the player assigns a different microphone test key
- **THEN** the new key starts or stops the test immediately and the old key no longer does

### Requirement: Microphone start state persists

The mod SHALL store a start-microphone boolean in its settings file and SHALL read it only when initializing a multiplayer world. The default value SHALL be disabled so a first run never opens the microphone without an explicit player choice.

#### Scenario: First run

- **WHEN** the mod runs for the first time
- **THEN** the settings file is created with the start-microphone setting disabled

#### Scenario: Later multiplayer session

- **WHEN** the start-microphone setting is enabled and a new multiplayer world loads
- **THEN** the session microphone starts enabled if the input device can be opened
