# Spec Delta

## MODIFIED Requirements

### Requirement: Volume falls off with distance

The mod SHALL reduce a speaker's playback volume as the distance in tiles between that speaker and the local player grows, and SHALL reach silence at the proximity radius declared by that speaker in the voice packet. The local player's own configured radius SHALL NOT gate incoming voice.

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
