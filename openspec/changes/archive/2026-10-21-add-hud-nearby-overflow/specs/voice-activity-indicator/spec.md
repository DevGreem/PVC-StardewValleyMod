# Spec Delta

## ADDED Requirements

### Requirement: Nearby list overflow is summarized

When more than seven players are eligible for the HUD nearby list, the mod SHALL show the seven nearest players and one summary row reporting the number of remaining nearby players. The summary SHALL read `+1 player nearby` for one remaining player and `+N players nearby` for more than one. The player-mute menu SHALL keep its existing rows and behavior.

#### Scenario: Seven nearby players

- **WHEN** exactly seven players are eligible for the nearby list
- **THEN** all seven are shown and no summary row is drawn

#### Scenario: Eight nearby players

- **WHEN** eight players are eligible for the nearby list
- **THEN** the seven nearest are shown followed by `+1 player nearby`

#### Scenario: Nine nearby players

- **WHEN** nine players are eligible for the nearby list
- **THEN** the seven nearest are shown followed by `+2 players nearby`

#### Scenario: Many nearby players

- **WHEN** twelve players are eligible for the nearby list
- **THEN** the seven nearest are shown followed by `+5 players nearby`

#### Scenario: Ranking

- **WHEN** more than seven players are eligible
- **THEN** the summary counts every eligible player beyond the seven shown, regardless of whether those additional players are speaking or muted
