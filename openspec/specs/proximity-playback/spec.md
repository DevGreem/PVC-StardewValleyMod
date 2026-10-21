# proximity-playback Specification

## Purpose
Turns received voice frames back into audible speech, with volume driven by how far away each speaker stands, so that talking to a nearby farmer feels local and talking across the map does not happen at all.

## Requirements

### Requirement: Same-location gating

The mod SHALL play voice only from players who are in the same game location as the local player.

#### Scenario: Speaker is in another location

- **WHEN** voice arrives from a player who is in a different game location
- **THEN** no audio is played for it

#### Scenario: Speaker leaves the location mid-sentence

- **WHEN** a player who was being heard walks out of the local player's location
- **THEN** their voice stops

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

### Requirement: Master playback volume

The mod SHALL apply the configured master playback volume to the final mixed output after the anti-clipping stage, so lowering the volume scales all incoming voice even when several loud speakers make the mix hit the limiter.

#### Scenario: Playback volume set to zero

- **WHEN** the player sets the playback volume to zero
- **THEN** no incoming voice is audible while everything else in the game is unaffected

#### Scenario: Playback volume changed during a loud mix

- **WHEN** the player lowers the playback volume while a loud speaker or several overlapping speakers are being limited
- **THEN** the output becomes proportionally quieter and remains within the audible range

### Requirement: Concurrent speakers are mixed

The mod SHALL play all in-range speakers simultaneously, and SHALL bound how many voices it mixes at once so that a crowded location cannot overload the audio pipeline.

#### Scenario: Several players speak at once

- **WHEN** multiple in-range players speak at the same time
- **THEN** all of them are heard together

#### Scenario: More speakers than the mod mixes at once

- **WHEN** more in-range players speak at once than the mod mixes simultaneously
- **THEN** the closest speakers are heard and the rest are left out for that moment

### Requirement: Overlapping voices do not clip

The mod SHALL keep the mixed output within the audible range when several voices overlap, rather than clipping.

#### Scenario: Many loud voices overlap

- **WHEN** several loud voices are mixed at the same time
- **THEN** the result stays within the audible range and does not clip

### Requirement: Playback absorbs packet loss and jitter

The mod SHALL buffer incoming voice so that short network jitter or a single lost packet does not produce an audible gap.

#### Scenario: A single packet is lost

- **WHEN** one voice packet never arrives
- **THEN** that speaker's audio continues without a noticeable break

#### Scenario: Packets arrive unevenly

- **WHEN** packets arrive with varying delay
- **THEN** playback stays continuous

### Requirement: Playback survives game hitches

The mod SHALL keep playing incoming voice while the game itself is busy.

#### Scenario: The game stutters or autosaves

- **WHEN** the game hitches, pauses or autosaves while another player is speaking
- **THEN** that player's voice keeps playing

### Requirement: Decoding failures do not interrupt playback

The mod SHALL discard a frame it cannot decode and continue playing that speaker's later frames.

#### Scenario: A frame fails to decode

- **WHEN** a received frame cannot be decoded
- **THEN** it is skipped and playback continues with the next frame

### Requirement: Output device selection

The mod SHALL play incoming voice through the player's selected output device, falling back to the system default output device when no valid device is selected.

#### Scenario: No output device chosen

- **WHEN** the player has not chosen an output device
- **THEN** the system default output device is used

#### Scenario: Selected output device disappears

- **WHEN** the previously selected output device is no longer present
- **THEN** the mod falls back to the system default and reports the substitution

### Requirement: No playback outside a multiplayer world

The mod SHALL NOT play incoming voice when no multiplayer world is loaded or after the player returns to the title screen.

#### Scenario: Returning to the title screen

- **WHEN** the player leaves the world
- **THEN** no incoming voice is played
