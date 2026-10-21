# Spec Delta

## Purpose

Determines which static obstacles block the line of sight between two farmers and how those obstacles attenuate, muffle or silence a speaker.

## ADDED Requirements

### Requirement: Static obstacle line of sight

The mod SHALL trace the line between the speaker and the listener and count the static obstacles it crosses. A contiguous run of solid tiles on the map's static buildings layer SHALL count as one obstacle; each constructed building (cabin, shed, barn, coop) SHALL count as one obstacle. Open or passable doors on the map layer SHALL NOT count.

#### Scenario: Clear line of sight

- **WHEN** no static obstacle lies between the speaker and the listener
- **THEN** the speaker is not occluded

#### Scenario: Wall between the farmers

- **WHEN** the line crosses one solid static wall
- **THEN** that wall counts as one obstacle

#### Scenario: Thick wall

- **WHEN** the line crosses several contiguous solid tiles of the same wall
- **THEN** the wall still counts as one obstacle

#### Scenario: Two separate walls

- **WHEN** the line crosses two wall runs separated by clear ground
- **THEN** they count as two obstacles

#### Scenario: Cabin between the farmers

- **WHEN** the line crosses one constructed cabin, shed, barn or coop
- **THEN** that building counts as one obstacle

#### Scenario: Open door

- **WHEN** the line crosses a door tile marked as passable on the map layer
- **THEN** the door does not count as an obstacle

#### Scenario: Dynamic objects are out of scope

- **WHEN** the line crosses a tree, fence, furniture or other player-placed object rather than a static map tile or a constructed building
- **THEN** that object does not count as an obstacle in this change

#### Scenario: Location without static geometry

- **WHEN** the current location has neither a static building layer nor constructed buildings
- **THEN** no static obstacle occlusion is applied

### Requirement: Obstacle levels

The mod SHALL attenuate and muffle a speaker according to the number of static obstacles crossed by the line: one obstacle makes the voice quieter and audibly muffled, two obstacles make it much quieter and heavily muffled, and three or more obstacles make it silent.

#### Scenario: One obstacle

- **WHEN** the line crosses exactly one wall or one constructed building
- **THEN** the speaker is quieter than in the open and the voice sounds muffled

#### Scenario: Cabin between the farmers

- **WHEN** the only obstacle is one cabin, shed, barn or coop
- **THEN** the speaker is still audible but quieter and muffled, not silent

#### Scenario: Two obstacles

- **WHEN** the line crosses exactly two separate walls or buildings
- **THEN** the speaker is much quieter and more heavily muffled than with one obstacle

#### Scenario: Three or more obstacles

- **WHEN** the line crosses three or more separate walls or buildings
- **THEN** the speaker is silent

### Requirement: Occlusion composes with distance

The mod SHALL apply static obstacle occlusion on top of the distance falloff, so a speaker behind an obstacle is never louder than the same speaker in the open at the same distance.

#### Scenario: Same distance, with and without an obstacle

- **WHEN** two speakers are at the same distance from the listener, one in the open and one behind an obstacle
- **THEN** the occluded speaker is quieter

#### Scenario: Occluded speaker outside the reach

- **WHEN** a speaker is beyond their declared voice reach
- **THEN** they remain silent regardless of occlusion

### Requirement: Host-controlled occlusion setting

The host SHALL be the only player who can enable or disable static obstacle occlusion. Clients SHALL follow the host's value and SHALL NOT override it locally. If the host does not run the mod and no host value is available, occlusion SHALL remain enabled.

#### Scenario: Host changes the setting

- **WHEN** the host enables or disables wall occlusion
- **THEN** every client applies the new value immediately

#### Scenario: Client tries to change the setting

- **WHEN** a client enables or disables wall occlusion locally
- **THEN** the client keeps following the host's value and the change is ignored

#### Scenario: Late joiner

- **WHEN** a player joins a session after the host has chosen a value
- **THEN** the host sends the current value and the new client applies it

#### Scenario: Host does not run the mod

- **WHEN** the host does not have PVC installed and no host value is received
- **THEN** wall occlusion remains enabled on every client and clients cannot change it

### Requirement: Local computation and packet independence

The mod SHALL compute the line of sight locally from each listener's map and the farmers' game-state positions. The voice packet SHALL NOT carry occlusion data; only the host's on/off preference is synchronized separately.

#### Scenario: Occlusion is not transmitted in voice

- **WHEN** a voice packet is sent
- **THEN** it contains no occlusion field and no wall information

#### Scenario: Clients compute their own line of sight

- **WHEN** two clients hear the same speaker from different positions
- **THEN** each client uses its own map and farmer positions to decide the attenuation and muffling
