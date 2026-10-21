# voice-capture Specification

## Purpose

Captures the local player's microphone and turns it into a stream of encoded voice frames, so the rest of the mod can decide when the player is speaking and what to transmit.

## Requirements

### Requirement: Microphone capture

The mod SHALL capture microphone audio from the player's selected input device as a continuous stream of fixed-duration frames while a multiplayer world is loaded and the microphone is enabled.

#### Scenario: Capturing during a multiplayer session

- **WHEN** the player is in a loaded multiplayer world and the microphone is enabled
- **THEN** the mod captures audio continuously from the selected input device

#### Scenario: Selected input device is unavailable

- **WHEN** the configured input device cannot be opened
- **THEN** the mod reports the failure, disables voice capture and leaves the game running

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

### Requirement: Push-to-talk activation

While push-to-talk is the selected activation mode, the mod SHALL transmit voice only during the interval in which the push-to-talk key is held down.

#### Scenario: Holding the push-to-talk key

- **WHEN** the player holds the push-to-talk key
- **THEN** captured voice is transmitted to the other players of the session

#### Scenario: Releasing the push-to-talk key

- **WHEN** the player releases the push-to-talk key
- **THEN** the mod stops transmitting voice

### Requirement: Voice-activity activation

While voice-activity is the selected activation mode, the mod SHALL transmit a frame only when that frame's loudness exceeds the configured sensitivity threshold.

#### Scenario: Speaking above the threshold

- **WHEN** the player speaks loudly enough to exceed the configured threshold
- **THEN** the frame is transmitted

#### Scenario: Staying below the threshold

- **WHEN** the input level remains below the configured threshold
- **THEN** no voice is transmitted

### Requirement: Microphone state change takes effect immediately

The mod SHALL apply a change of microphone state on the next captured frame, without requiring the player to restart the game or reload the world.

#### Scenario: Muting mid-sentence

- **WHEN** the player disables the microphone while speaking
- **THEN** transmission stops on the next frame and no further voice leaves the machine

#### Scenario: Unmuting

- **WHEN** the player enables the microphone again
- **THEN** transmission resumes without a restart

### Requirement: No capture outside a multiplayer world

The mod SHALL NOT capture or transmit voice when the local player is in single-player, when no world is loaded, or after returning to the title screen, except while microphone test mode is explicitly active in a loaded world. Test mode SHALL capture only locally and SHALL NOT transmit.

#### Scenario: Playing alone

- **WHEN** the player is in a single-player save without microphone test mode
- **THEN** the microphone is not opened and nothing is transmitted

#### Scenario: Returning to the title screen

- **WHEN** the player leaves the world and returns to the title screen
- **THEN** capture, test capture and transmission stop

#### Scenario: Microphone test mode is active

- **WHEN** the player enables microphone test mode in a loaded single-player or multiplayer world
- **THEN** the microphone opens for local test feedback and no voice is transmitted

### Requirement: Encoding failures do not interrupt play

The mod SHALL drop a frame that cannot be encoded and continue capturing, without raising an error to the player or interrupting the game.

#### Scenario: A frame fails to encode

- **WHEN** encoding a captured frame produces no output or fails
- **THEN** that frame is discarded and capture continues with the next frame

### Requirement: Microphone selection

The mod SHALL let the player choose which input device is captured, and SHALL fall back to the system default device when no valid device is selected.

#### Scenario: No device selected

- **WHEN** the player has not chosen an input device
- **THEN** the system default input device is used

#### Scenario: Device list changes between sessions

- **WHEN** the previously selected input device is no longer present
- **THEN** the mod falls back to the system default input device and reports the substitution
