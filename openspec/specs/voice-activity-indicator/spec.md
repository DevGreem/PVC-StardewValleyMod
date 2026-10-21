# voice-activity-indicator Specification

## Purpose
Gives the player immediate on-screen feedback about who can hear them and who they can hear, so that the microphone state and the reach of their voice are never a guess.

## Requirements

### Requirement: Local microphone state is visible

The mod SHALL show on screen whether the local player's microphone is currently transmitting or muted.

#### Scenario: Player is transmitting

- **WHEN** the microphone is enabled and the player is transmitting
- **THEN** the indicator shows that the player is speaking

#### Scenario: Player is muted

- **WHEN** the microphone is disabled
- **THEN** the indicator makes the muted state visible

### Requirement: Nearby speakers are indicated

The mod SHALL indicate which nearby players are currently speaking.

#### Scenario: Another player speaks nearby

- **WHEN** a player within the proximity radius speaks
- **THEN** the indicator shows that player as speaking

#### Scenario: A speaker stops talking

- **WHEN** a player stops speaking
- **THEN** the indicator stops showing them as speaking shortly afterwards

### Requirement: Only in-range players are indicated

The mod SHALL indicate only players who are in the local player's game location and within the proximity radius declared by that speaker. The local player's own configured radius SHALL NOT filter incoming speakers.

#### Scenario: A player walks out of range

- **WHEN** a player moves beyond the proximity radius declared by that player
- **THEN** they are no longer indicated as nearby

#### Scenario: A player is in another location

- **WHEN** a player is elsewhere on the map
- **THEN** they are not indicated at all

#### Scenario: The local player has a smaller radius

- **WHEN** a nearby speaker declares a radius larger than the local player's configured radius
- **THEN** the speaker remains indicated while inside the speaker's declared radius

### Requirement: Active proximity radius is visible

The mod SHALL display the local player's active outgoing voice radius.

#### Scenario: Player cycles the radius

- **WHEN** the player changes the proximity radius
- **THEN** the indicator reflects the new outgoing reach

### Requirement: Indicator appears only in multiplayer

The mod SHALL NOT display voice indicators when no multiplayer session is active.

#### Scenario: Playing alone

- **WHEN** the player is in a single-player save
- **THEN** no voice indicator is displayed

### Requirement: Indicator can be hidden

The mod SHALL let the player hide the indicator.

#### Scenario: Player hides the indicator

- **WHEN** the player turns the indicator off in the settings
- **THEN** it is not drawn for the rest of the session

### Requirement: Indicator does not interfere with play

The mod SHALL draw the indicator without capturing clicks or keystrokes intended for the game.

#### Scenario: Clicking where the indicator is drawn

- **WHEN** the player clicks on the area where the indicator appears
- **THEN** the click reaches the game exactly as it normally would
