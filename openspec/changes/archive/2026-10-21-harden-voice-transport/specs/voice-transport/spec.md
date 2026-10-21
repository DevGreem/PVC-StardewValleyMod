# Spec Delta

## ADDED Requirements

### Requirement: Voice is sent only to eligible nearby peers

The mod SHALL address an outgoing voice packet only to peers that are in the same game location as the sender and inside the reach declared by that packet. It SHALL NOT address the packet to unrelated, remotely located or out-of-range peers. This is a traffic and privacy mitigation, not an authentication or confidentiality guarantee.

#### Scenario: Peer in range and in the same location

- **WHEN** a voice packet is sent to a peer in the sender's location and inside the packet's declared reach
- **THEN** that peer is included as a recipient

#### Scenario: Peer outside the reach

- **WHEN** a peer in the same location is outside the packet's declared reach
- **THEN** that peer is not a recipient

#### Scenario: Peer in another location

- **WHEN** a peer is in a different game location
- **THEN** that peer is not a recipient

#### Scenario: No eligible recipients

- **WHEN** no peer is in range and in the same location
- **THEN** the packet is not sent

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
