# Spec Delta

## Purpose

Applies location-based acoustic presets so enclosed environments, starting with caves and mines, sound different from open-air locations.

## ADDED Requirements

### Requirement: Cave locations reduce and muffle voice

The mod SHALL apply a quieter and more muffled playback preset when the listener is in a mine or a farm cave. The cave preset SHALL be independent of the host's wall-occlusion setting.

#### Scenario: Listening in a mine

- **WHEN** the local player is in a mine shaft
- **THEN** incoming voice is quieter and muffled

#### Scenario: Listening in a farm cave

- **WHEN** the local player is in the farm cave or the island farm cave
- **THEN** incoming voice is quieter and muffled

#### Scenario: Wall occlusion disabled

- **WHEN** the host disables wall occlusion but the listener is in a cave
- **THEN** the cave preset still applies

### Requirement: Cave preset composes with distance and obstacles

The mod SHALL apply the cave preset on top of distance falloff and static obstacle occlusion, so a distant or occluded speaker in a cave is never louder than the same speaker in the open.

#### Scenario: Distant speaker in a cave

- **WHEN** a speaker is far away but still inside the declared voice reach while the listener is in a cave
- **THEN** the cave preset makes the voice quieter than the same distance in the open

#### Scenario: Speaker behind an obstacle in a cave

- **WHEN** the line crosses a static obstacle while the listener is in a cave
- **THEN** both the obstacle and the cave preset reduce the voice
