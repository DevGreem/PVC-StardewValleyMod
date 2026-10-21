# Spec Delta

## ADDED Requirements

### Requirement: Remote player names are sanitized

The mod SHALL sanitize a remote player's name before displaying it in the HUD or mute menu, writing it to logs or saving it in the local mute list. Sanitization SHALL trim surrounding whitespace, remove control characters, cap the visible length and use a stable fallback when nothing usable remains.

#### Scenario: Name contains control characters

- **WHEN** a remote player's name contains newlines or other control characters
- **THEN** the displayed, logged and saved name contains none of them

#### Scenario: Name is very long

- **WHEN** a remote player's name exceeds the display cap
- **THEN** only the capped visible portion is used

#### Scenario: Name is empty after sanitization

- **WHEN** a remote player's name is empty or consists only of removed characters
- **THEN** the mod uses `Player <id>` as the display and persistence fallback
