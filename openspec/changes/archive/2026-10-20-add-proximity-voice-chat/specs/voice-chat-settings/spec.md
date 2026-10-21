# Spec Delta

## Purpose

Holds the mod's configuration and exposes it to the player, so that devices, volume, range, activation mode and keybinds can be changed in-game without editing files or restarting.

## ADDED Requirements

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

When Generic Mod Config Menu is installed, the mod SHALL register a settings panel exposing the input device, output device, playback volume, proximity radius, activation mode, push-to-talk key and microphone key.

#### Scenario: Panel is available

- **WHEN** Generic Mod Config Menu is installed
- **THEN** the player can view and change every setting from the in-game panel

### Requirement: Settings panel is optional

The mod SHALL run with its saved and default settings when Generic Mod Config Menu is not installed.

#### Scenario: Generic Mod Config Menu is missing

- **WHEN** Generic Mod Config Menu is not installed
- **THEN** the mod loads and runs using the values in its settings file

### Requirement: Settings changes apply without a restart

The mod SHALL apply changes to playback volume, proximity radius, activation mode and audio devices immediately, without requiring a game restart or a world reload.

#### Scenario: Player changes the input device

- **WHEN** the player selects a different microphone in the panel
- **THEN** capture moves to that device immediately

#### Scenario: Player changes the proximity radius

- **WHEN** the player changes the proximity radius
- **THEN** the new radius governs incoming voice immediately

### Requirement: Proximity radius is chosen from a fixed set

The mod SHALL offer a small fixed set of proximity radii and SHALL NOT accept an arbitrary value.

#### Scenario: Player cycles the radius

- **WHEN** the player cycles the proximity radius with the configured key
- **THEN** the radius moves to the next offered value

### Requirement: Microphone toggle keybind

The mod SHALL provide a keybind that toggles the microphone during play.

#### Scenario: Toggling the microphone

- **WHEN** the player presses the microphone keybind
- **THEN** the microphone state flips and the new state is saved

### Requirement: Settings can be reset to defaults

The mod SHALL provide a way to restore every setting to its documented default value.

#### Scenario: Player resets the settings

- **WHEN** the player resets the settings
- **THEN** every setting returns to its documented default

### Requirement: Keybinds are player-configurable

The mod SHALL let the player choose the push-to-talk key, the proximity cycle key and the microphone toggle key, and SHALL accept any key the game itself can bind.

#### Scenario: Player rebinds push-to-talk

- **WHEN** the player assigns a different push-to-talk key
- **THEN** the new key takes effect immediately and the old one no longer transmits
