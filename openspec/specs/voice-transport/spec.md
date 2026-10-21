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

The mod SHALL discard a packet whose declared proximity radius is neither one of the current offered values (`3`, `15`, `30`) nor one of the legacy values (`6`, `10`, `20`) accepted for compatibility. The sender SHALL only advertise current offered values.

#### Scenario: Unsupported radius declared

- **WHEN** a packet declares a proximity radius outside both the current and legacy sets
- **THEN** the packet is discarded

#### Scenario: Current radius declared

- **WHEN** a packet declares `3`, `15` or `30`
- **THEN** the packet is accepted for proximity evaluation

#### Scenario: Legacy radius declared

- **WHEN** a packet declares `6`, `10` or `20` from an older build
- **THEN** the packet is accepted for proximity evaluation instead of being discarded

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

### Requirement: Voice is sent only to eligible nearby peers

The mod SHALL address an outgoing voice packet to peers in the same game location as the sender and inside the reach declared by that packet. It SHALL NOT broadcast the packet to unrelated or remotely located peers.

#### Scenario: Peer in range and in the same location

- **WHEN** a voice packet is sent to a peer in the sender's location and inside the packet's declared reach
- **THEN** that peer is included as a recipient

#### Scenario: Peer outside the reach

- **WHEN** a peer in the same location is outside the packet's declared reach
- **THEN** that peer is not a recipient

#### Scenario: Peer in another location

- **WHEN** a peer is in a different game location
- **THEN** that peer is not a recipient

#### Scenario: No eligible recipients on the host

- **WHEN** the host has no peers in range and in the same location
- **THEN** the packet is not sent

### Requirement: Client voice reaches the session host

On a farmhand client, the host SHALL also receive outgoing voice as session authority, even if its replicated position is briefly stale. The host applies its own location and range gate before playback.

#### Scenario: Client sends to the host

- **WHEN** a farmhand sends a voice packet and the host is the session authority
- **THEN** the host is included as a recipient even if its replicated tile is temporarily stale

### Requirement: Unknown voice senders are rejected before state allocation

The mod SHALL discard a voice message whose sender ID is not a current farmer before creating per-sender rate-limit or decoding state.

#### Scenario: Spoofed sender ID

- **WHEN** a voice message arrives with an ID that is not in the current farmer list
- **THEN** it is discarded before a rate limiter is created

### Requirement: Encoded voice payload size is bounded before decoding

The mod SHALL reject a voice message whose Base64 `Data` is longer than the maximum valid encoded voice packet before attempting to decode it.

#### Scenario: Oversized encoded payload

- **WHEN** a voice message carries a Base64 string larger than the maximum valid packet
- **THEN** it is discarded before `Convert.FromBase64String` allocates the decoded buffer

### Requirement: Voice attribution is advisory

The mod SHALL treat the sender ID supplied by SMAPI as unauthenticated and SHALL NOT claim that it proves who produced the voice. Documentation SHALL state that voice attribution and local mute are advisory.

#### Scenario: Modified client claims another ID

- **WHEN** a modified client sends a voice message with another player's claimed ID
- **THEN** the mod may attribute the audio to that claimed player; this limitation is documented rather than presented as authenticated identity
