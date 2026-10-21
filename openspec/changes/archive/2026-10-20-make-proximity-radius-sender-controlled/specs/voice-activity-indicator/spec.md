# Spec Delta

## MODIFIED Requirements

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
