# Spec Delta

## MODIFIED Requirements

### Requirement: In-game settings panel

When Generic Mod Config Menu is installed, the mod SHALL register a settings panel exposing the input device, output device, playback volume, proximity radius (presented as the local voice reach), activation mode, the persisted start-microphone setting, push-to-talk key, microphone toggle key, radius cycle key, microphone test key, player-mute menu key, local-monitor toggle and host-controlled wall-occlusion toggle. It SHALL expose the current session microphone state and the microphone test toggle as separate runtime controls that do not modify the persisted start-microphone setting. The wall-occlusion control SHALL be usable only by the host; clients SHALL see the host's effective value and SHALL NOT be able to change it. It SHALL also show a paragraph explaining that the player-mute list is local and opened with the menu key.

#### Scenario: Panel is available

- **WHEN** Generic Mod Config Menu is installed
- **THEN** the player can view and change every setting from the in-game panel, including the start-microphone setting, the current session microphone state, the microphone test controls, the player-mute menu key and the host-controlled wall-occlusion value

#### Scenario: Client opens the panel

- **WHEN** a non-host player opens the wall-occlusion control
- **THEN** the control shows the host's effective value and does not allow the client to change it

### Requirement: Settings changes apply without a restart

The mod SHALL apply changes to playback volume, proximity radius, activation mode, audio devices, microphone test key, player-mute menu key and local-monitor toggle immediately, without requiring a game restart or a world reload. Changing the proximity radius SHALL change the local player's outgoing voice reach immediately. Changing the host's wall-occlusion setting SHALL apply to every player immediately; a client's attempt to change it SHALL be ignored. Muting or unmuting a player SHALL apply to local playback immediately. Changing the start-microphone setting SHALL take effect on the next multiplayer world load and SHALL NOT change the microphone state of the current session.

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

#### Scenario: Player changes the start-microphone setting

- **WHEN** the player changes the persisted start-microphone setting during a session
- **THEN** the current session microphone state is unchanged and the new value is used the next time a multiplayer world loads

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
