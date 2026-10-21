# Spec Delta

## MODIFIED Requirements

### Requirement: In-game settings panel

When Generic Mod Config Menu is installed, the mod SHALL register a settings panel exposing the input device, output device, playback volume, proximity radius (presented as the local voice reach), activation mode, the persisted start-microphone setting, push-to-talk key, microphone toggle key, radius cycle key, microphone test key and local-monitor toggle. It SHALL expose the current session microphone state and the microphone test toggle as separate runtime controls that do not modify the persisted start-microphone setting.

#### Scenario: Panel is available

- **WHEN** Generic Mod Config Menu is installed
- **THEN** the player can view and change every setting from the in-game panel, including the start-microphone setting, the current session microphone state and the microphone test controls

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
