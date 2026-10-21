# Spec Delta

## MODIFIED Requirements

### Requirement: Volume falls off with distance

The mod SHALL reduce a speaker's playback volume as the distance in tiles between that speaker and the local player grows, and SHALL reach silence at the proximity radius declared by that speaker in the voice packet. The local player's own configured radius SHALL NOT gate incoming voice. Static obstacle occlusion and location acoustic presets SHALL further reduce the gain, and a speaker fully occluded by static obstacles SHALL be silent.

#### Scenario: Speaker is close by

- **WHEN** the speaker stands next to the local player
- **THEN** their voice plays at the full configured volume

#### Scenario: Speaker is at the edge of the radius

- **WHEN** the speaker is at or beyond the proximity radius declared by that speaker
- **THEN** their voice is silent

#### Scenario: Speaker walks away while talking

- **WHEN** the speaker moves from close by to the edge of the radius declared by that speaker
- **THEN** their voice gets progressively quieter

#### Scenario: Listener has a smaller configured radius

- **WHEN** the local player's own radius is smaller than the radius declared by the speaker
- **THEN** the speaker is still audible inside the speaker's declared radius

#### Scenario: Speaker behind an obstacle

- **WHEN** the speaker is at the same distance as an unoccluded speaker but a static wall or constructed building crosses the line between them
- **THEN** the occluded speaker is quieter and muffled

#### Scenario: Speaker behind enough obstacles

- **WHEN** enough static obstacles cross the line between the speaker and the listener to make the occlusion total
- **THEN** the speaker is silent

#### Scenario: Listener in a cave

- **WHEN** the listener is in a mine or farm cave
- **THEN** incoming voice is quieter and muffled even when there are no walls between the farmers
