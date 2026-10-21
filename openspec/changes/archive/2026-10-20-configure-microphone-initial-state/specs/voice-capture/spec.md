# Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Microphone starts disabled`
- TO: `### Requirement: Microphone initial state`

## MODIFIED Requirements

### Requirement: Microphone initial state

When a multiplayer world finishes loading, the mod SHALL initialize the session microphone from the persisted `StartMicrophoneEnabled` setting. A false value SHALL leave the microphone disabled; a true value SHALL enable it and start capture when the input device can be opened. The setting SHALL NOT open the microphone in single-player.

#### Scenario: Session begins

- **WHEN** a player joins or hosts a multiplayer session and `StartMicrophoneEnabled` is false
- **THEN** the microphone starts disabled and no voice is transmitted until the player enables it

#### Scenario: Session begins with the setting enabled

- **WHEN** a player joins or hosts a multiplayer session and `StartMicrophoneEnabled` is true
- **THEN** the microphone is enabled and capture starts without requiring the toggle key

#### Scenario: Setting enabled but microphone cannot open

- **WHEN** `StartMicrophoneEnabled` is true and the selected input device cannot be opened
- **THEN** the mod reports the failure, leaves the session microphone disabled and keeps the game running

#### Scenario: Player enables the microphone

- **WHEN** the player enables the microphone from the settings panel or the microphone keybind
- **THEN** capture and transmission become possible for the rest of the session

#### Scenario: Single-player world

- **WHEN** the player loads a single-player save
- **THEN** the microphone is not opened regardless of `StartMicrophoneEnabled`
