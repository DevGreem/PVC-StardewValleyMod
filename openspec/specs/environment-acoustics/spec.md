# environment-acoustics Specification

## Purpose

Applies location-based acoustic presets so enclosed environments, starting with caves and mines, sound different from open-air locations.

## Requirements

### Requirement: Cave locations reduce and muffle voice

The mod SHALL apply a short echo/reverb and a mild attenuation to incoming voice when the listener is in a mine or a farm cave. The dry voice SHALL remain intelligible; the cave effect SHALL be independent of the host's wall-occlusion setting.

#### Scenario: Listening in a mine

- **WHEN** the local player is in a mine shaft
- **THEN** incoming voice has an audible short echo and remains intelligible

#### Scenario: Listening in a farm cave

- **WHEN** the local player is in the farm cave or the island farm cave
- **THEN** incoming voice has an audible short echo and remains intelligible

#### Scenario: Wall occlusion disabled

- **WHEN** the host disables wall occlusion but the listener is in a cave
- **THEN** the cave echo still applies

#### Scenario: Outside a cave

- **WHEN** the listener is not in a cave or mine
- **THEN** no cave echo is applied

### Requirement: Cave preset composes with distance and obstacles

The mod SHALL apply the cave echo on top of distance falloff and static obstacle occlusion, so a distant or occluded speaker in a cave is never louder than the same speaker in the open. The echo SHALL NOT bypass the distance or obstacle gains.

#### Scenario: Distant speaker in a cave

- **WHEN** a speaker is far away but still inside the declared voice reach while the listener is in a cave
- **THEN** the voice has the cave echo and is quieter than the same distance in the open

#### Scenario: Speaker behind an obstacle in a cave

- **WHEN** the line crosses a static obstacle while the listener is in a cave
- **THEN** both the obstacle attenuation and the cave echo apply

### Requirement: Cave echo is local and keeps the test monitor clean

The mod SHALL apply the cave echo only to remote voice playback. It SHALL NOT apply to the local microphone test monitor and SHALL NOT be transmitted to other players.

#### Scenario: Microphone test in a cave

- **WHEN** the local microphone test is active while the player is in a cave
- **THEN** the monitor plays a clean local signal without the cave echo

#### Scenario: Other players are outside the cave

- **WHEN** one listener is in a cave and another player is outside
- **THEN** only the listener hears the cave echo; the other player's playback is unchanged
