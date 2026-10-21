# Spec Delta

## ADDED Requirements

### Requirement: Keybinds are inactive while typing

The mod SHALL ignore its gameplay keybinds and SHALL NOT transmit push-to-talk while the chat box is active.

#### Scenario: Player types the push-to-talk key in chat

- **WHEN** the chat box is active and the player presses the push-to-talk key
- **THEN** the microphone does not transmit and the character reaches the chat box

#### Scenario: Player presses another PVC keybind in chat

- **WHEN** the chat box is active and the player presses a PVC microphone, test, radius or player-mute keybind
- **THEN** that action does not fire and the key is left to the chat input

#### Scenario: Player closes chat

- **WHEN** the chat box is closed after typing
- **THEN** the PVC keybinds work normally again
