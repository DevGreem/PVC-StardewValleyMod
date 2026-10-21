# Spec Delta

## MODIFIED Requirements

### Requirement: Master playback volume

The mod SHALL apply the configured master playback volume to the final mixed output after the anti-clipping stage, so lowering the volume scales all incoming voice even when several loud speakers make the mix hit the limiter.

#### Scenario: Playback volume set to zero

- **WHEN** the player sets the playback volume to zero
- **THEN** no incoming voice is audible while everything else in the game is unaffected

#### Scenario: Playback volume changed during a loud mix

- **WHEN** the player lowers the playback volume while a loud speaker or several overlapping speakers are being limited
- **THEN** the output becomes proportionally quieter and remains within the audible range
