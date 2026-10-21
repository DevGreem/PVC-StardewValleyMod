# voice-transport Specification

## Purpose
Carries encoded voice frames between the players of a session over the game's own multiplayer network, with the validation and rate limiting needed to keep one misbehaving player from ruining everyone else's audio.

## Requirements

### Requirement: Voice packet contents

Every transmitted voice packet SHALL carry a wire-format version, a sequence number, the sender's proximity radius, the sender's location name and one encoded audio frame.

#### Scenario: A frame is transmitted

- **WHEN** the microphone produces a frame that should be sent
- **THEN** the packet includes the version, sequence number, proximity radius, location name and audio payload

### Requirement: Wire-format versioning

The mod SHALL declare a wire-format version in every packet and SHALL discard packets whose version it does not support.

#### Scenario: Peers run incompatible mod versions

- **WHEN** a packet arrives with an unsupported wire-format version
- **THEN** the packet is discarded and no audio is played for it

### Requirement: Voice is sent only within the session

The mod SHALL address voice packets only to the players of the current multiplayer session, and SHALL ignore any mod message that does not come from this mod.

#### Scenario: Message from an unrelated mod

- **WHEN** a mod message arrives from a different mod
- **THEN** it is ignored

#### Scenario: Own packet received back

- **WHEN** the mod receives a voice packet that it sent itself
- **THEN** it is ignored and never played back

### Requirement: Sender position comes from game state

The mod SHALL determine a speaker's position from the game's own player state and SHALL NOT trust position data carried inside a packet.

#### Scenario: Packet is evaluated for proximity

- **WHEN** a voice packet is received
- **THEN** the speaker's position used for proximity is read from the game's farmer state for that player

#### Scenario: Sender is not a known player

- **WHEN** a packet arrives from a player who is not present in the game's farmer state
- **THEN** the packet is discarded

### Requirement: Proximity radius is validated

The mod SHALL discard any packet whose declared proximity radius is not one of the radius values the mod itself offers.

#### Scenario: Unsupported radius declared

- **WHEN** a packet declares a proximity radius outside the offered set
- **THEN** the packet is discarded

### Requirement: Incoming packets are structurally validated

The mod SHALL discard a packet that is malformed, that exceeds the maximum accepted size, or that carries an empty or over-long location name, and SHALL continue running normally.

#### Scenario: Malformed payload

- **WHEN** a received packet cannot be parsed
- **THEN** it is discarded and no audio is produced

#### Scenario: Oversized audio payload

- **WHEN** a packet's audio payload is larger than the maximum accepted size
- **THEN** it is discarded

#### Scenario: Location name out of bounds

- **WHEN** a packet carries an empty location name or one longer than the maximum accepted length
- **THEN** it is discarded

### Requirement: Per-sender rate limiting

The mod SHALL cap the number of voice packets accepted from any single sender per second and SHALL drop the excess.

#### Scenario: A peer floods the session

- **WHEN** one player sends substantially more packets per second than a speaking player legitimately would
- **THEN** the excess is dropped and the other players' audio is unaffected

#### Scenario: Normal speech is not throttled

- **WHEN** a player sends packets at the rate normal speech produces
- **THEN** all of them are accepted

### Requirement: Disconnecting players are released immediately

The mod SHALL release a player's buffered audio and rate-limit state as soon as that player disconnects.

#### Scenario: A player leaves the session

- **WHEN** a peer disconnects
- **THEN** their pending voice state is dropped without waiting for a timeout
