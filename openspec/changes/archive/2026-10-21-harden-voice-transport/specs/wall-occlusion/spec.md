# Spec Delta

## ADDED Requirements

### Requirement: Settings messages are rate limited

The mod SHALL apply a global rate limit to incoming wall-occlusion settings messages before parsing them, so one peer cannot make the host answer an unbounded request flood.

#### Scenario: Peer floods settings requests

- **WHEN** a peer sends substantially more wall-occlusion messages per second than legitimate joins or setting changes produce
- **THEN** the excess is dropped before parsing and the host does not answer every request

#### Scenario: Legitimate settings message

- **WHEN** a peer joins or the host changes the setting
- **THEN** the message is processed normally within the budget
