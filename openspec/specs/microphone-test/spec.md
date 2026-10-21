# microphone-test Specification

## Purpose

Lets a player verify the selected microphone locally, with an input-level meter and optional sidetone monitoring, without sending audio to other players or recording it.

## Requirements

### Requirement: Explicit microphone test mode

The mod SHALL provide a microphone test mode that starts disabled and is enabled or disabled only by an explicit player action from the settings panel or a configurable keybind. The active state SHALL NOT be persisted between sessions.

#### Scenario: Player enables test mode

- **WHEN** the player enables microphone test mode in a loaded world
- **THEN** local capture starts and the HUD indicates that test mode is active

#### Scenario: Player disables test mode

- **WHEN** the player disables microphone test mode
- **THEN** local test capture stops and the previous normal voice state is restored

### Requirement: Test mode captures locally only

While microphone test mode is active, the mod SHALL capture the selected input device and SHALL NOT create voice packets, transmit test audio to other players or play remote voice. Test audio SHALL remain on the local machine.

#### Scenario: Test mode in single-player

- **WHEN** the player enables microphone test mode in a single-player world
- **THEN** the microphone opens locally for the test and nothing is transmitted

#### Scenario: No test audio reaches other players

- **WHEN** microphone test mode is active in a multiplayer world
- **THEN** no voice packets are sent for test audio

### Requirement: Input level is visible

While microphone test mode is active, the mod SHALL display the local input level and update it continuously from captured samples.

#### Scenario: Speaking raises the level

- **WHEN** the player speaks into the selected microphone
- **THEN** the displayed input level rises

#### Scenario: Silence lowers the level

- **WHEN** the microphone captures silence
- **THEN** the displayed input level stays near zero

### Requirement: Local monitoring is optional

The mod SHALL play captured test audio through the selected output device only when local monitoring is enabled, SHALL default monitoring to disabled, SHALL apply the configured playback volume to the monitor and SHALL stop monitoring when test mode ends.

#### Scenario: Monitoring disabled

- **WHEN** microphone test mode is active with monitoring disabled
- **THEN** the test audio is not played through the output device

#### Scenario: Monitoring enabled

- **WHEN** the player enables monitoring during microphone test mode
- **THEN** the captured audio is played locally through the selected output device at the configured playback volume

### Requirement: Test mode is temporary and private

Microphone test mode SHALL stop when the player leaves the world, returns to the title screen, disables the mode or closes the game. It SHALL NOT record audio, write voice data to disk or leave the microphone open after stopping.

#### Scenario: Returning to the title screen

- **WHEN** the player leaves the world while test mode is active
- **THEN** test capture and monitoring stop

#### Scenario: No recording

- **WHEN** microphone test mode is active
- **THEN** no audio is recorded or written to disk

### Requirement: Normal voice chat is paused during a test

While microphone test mode is active, the mod SHALL suspend normal voice capture, transmission and remote playback. When test mode ends, it SHALL restore the previous microphone and playback state if the world and settings still allow it.

#### Scenario: Test mode pauses voice chat

- **WHEN** the player enables microphone test mode during a multiplayer session
- **THEN** normal voice transmission and remote playback are paused

#### Scenario: Voice chat resumes after the test

- **WHEN** the player disables test mode in the same session
- **THEN** the previous microphone state and remote playback are restored
