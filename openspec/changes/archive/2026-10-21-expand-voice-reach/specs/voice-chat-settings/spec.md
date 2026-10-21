# Spec Delta

## MODIFIED Requirements

### Requirement: Proximity radius is chosen from a fixed set

The mod SHALL offer exactly the proximity radii `3`, `15` and `30` tiles, SHALL use `15` as the documented default, and SHALL NOT accept an arbitrary value. On load, `3` and `15` remain unchanged, while legacy `6`, `10` and `20` normalize to `3`, `15` and `30` respectively; any other value normalizes to the default.

#### Scenario: Player cycles the radius

- **WHEN** the player cycles the proximity radius with the configured key
- **THEN** the radius moves through `3`, `15`, `30` and back to `3`

#### Scenario: Old configuration contains a removed radius

- **WHEN** the settings file contains `6`, `10` or `20` from an older build
- **THEN** the loaded radius becomes `3`, `15` or `30` respectively and no error is reported

#### Scenario: Old configuration already uses a valid legacy value

- **WHEN** the settings file contains `3` or `15`
- **THEN** the loaded radius is preserved

#### Scenario: Configuration contains an arbitrary value

- **WHEN** the settings file contains a radius outside the current and legacy sets
- **THEN** the loaded radius becomes the default `15` and no error is reported
