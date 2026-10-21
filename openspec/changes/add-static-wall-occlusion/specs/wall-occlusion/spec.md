# Spec Delta

## Purpose

Determines whether static map walls block the line of sight between two farmers, so that voice can be quieter, muffled or silent behind solid geometry.

## ADDED Requirements

### Requirement: Static wall line of sight

The mod SHALL trace the line between the speaker and the listener through the current location's static building geometry and count the solid tiles that line crosses. Only solid tiles on the map's static buildings layer SHALL count; open or passable doors SHALL NOT count.

#### Scenario: Clear line of sight

- **WHEN** no solid static tile lies between the speaker and the listener
- **THEN** the speaker is not occluded

#### Scenario: Wall between the farmers

- **WHEN** the line crosses a solid static wall tile
- **THEN** the speaker is occluded

#### Scenario: Open door

- **WHEN** the line crosses a door tile marked as passable
- **THEN** the door does not count as an occluder

#### Scenario: Dynamic objects are out of scope

- **WHEN** the line crosses a tree, fence, furniture or other player-placed object rather than a static map tile
- **THEN** that object does not count as an occluder in this change

#### Scenario: Location without static geometry

- **WHEN** the current location has no static building layer
- **THEN** no static wall occlusion is applied

### Requirement: Occlusion levels

The mod SHALL attenuate and muffle a speaker according to the number of solid static tiles crossed by the line: one tile makes the voice quieter and audibly muffled, two tiles make it much quieter and heavily muffled, and three or more tiles make it silent.

#### Scenario: One wall

- **WHEN** the line crosses exactly one solid static tile
- **THEN** the speaker is quieter than in the open and the voice sounds muffled

#### Scenario: Two walls

- **WHEN** the line crosses exactly two solid static tiles
- **THEN** the speaker is much quieter and more heavily muffled than with one wall

#### Scenario: Three or more walls

- **WHEN** the line crosses three or more solid static tiles
- **THEN** the speaker is silent

### Requirement: Occlusion composes with distance

The mod SHALL apply static wall occlusion on top of the distance falloff, so a speaker behind a wall is never louder than the same speaker in the open at the same distance.

#### Scenario: Same distance, with and without a wall

- **WHEN** two speakers are at the same distance from the listener, one in the open and one behind a wall
- **THEN** the occluded speaker is quieter

#### Scenario: Occluded speaker outside the reach

- **WHEN** a speaker is beyond their declared voice reach
- **THEN** they remain silent regardless of occlusion

### Requirement: Local and packet-independent

The mod SHALL compute occlusion locally from the listener's map and the farmers' game-state positions. The voice packet SHALL NOT carry occlusion data, and disabling occlusion locally SHALL NOT change what other players hear.

#### Scenario: Occlusion is not transmitted

- **WHEN** a voice packet is sent
- **THEN** it contains no occlusion field and no wall information

#### Scenario: Listener disables occlusion

- **WHEN** a player disables wall occlusion
- **THEN** that player hears through static walls while other players' playback is unaffected
