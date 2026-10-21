# Spec Delta

## MODIFIED Requirements

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
