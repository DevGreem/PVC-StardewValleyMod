# player-mute Specification

## Purpose
Lets each player silence specific farmers locally, with a persisted mute list and a menu that covers both nearby players and everyone currently connected.

## Requirements

### Requirement: Local per-player mute

When the local player mutes another player, the mod SHALL NOT play that player's voice on the local machine. Unmuting SHALL restore local playback on the next available frame. Other players' playback SHALL NOT be affected, and the remote player's microphone SHALL remain active for everyone else.

#### Scenario: Mute a speaking player

- **WHEN** the local player mutes a player who is speaking
- **THEN** that player's voice stops locally and the other players continue to hear them normally

#### Scenario: Unmute a player

- **WHEN** the local player unmutes a player
- **THEN** that player's voice becomes audible locally again on the next available frame

#### Scenario: Muted player is far away

- **WHEN** a player is muted while outside the local voice reach and later comes close
- **THEN** they remain silent locally until the local player unmutes them

### Requirement: Persisted mute list

The mod SHALL store the local player's mute list in the local config, keyed by `UniqueMultiplayerID` and keeping the last known name for display. The list SHALL survive restarts and sessions, normalize duplicate or invalid entries, and be cleared when settings are reset to defaults.

#### Scenario: Mute survives a restart

- **WHEN** the player mutes someone and starts the game again later
- **THEN** that player is still muted locally

#### Scenario: Duplicate or invalid saved entries

- **WHEN** the config contains duplicate IDs, invalid IDs or empty names
- **THEN** the loaded list is normalized without preventing the mod from starting

#### Scenario: Saved player is offline

- **WHEN** a saved muted player is not currently online
- **THEN** the entry remains saved and has no effect until that player joins again

#### Scenario: Reset settings

- **WHEN** the player resets the settings to defaults
- **THEN** the local mute list is cleared

### Requirement: Nearby player menu

The mod SHALL provide a player-mute menu opened by a configurable keybind, with a Nearby view listing players in the same game location within the voice reach declared by each player. The local player's own outgoing reach SHALL NOT filter this view. Until a player's reach is known, the view SHALL use the maximum offered reach as a fallback. Each row SHALL show the player name, location, own voice reach and mute state, and selecting a row SHALL toggle that player's mute and save the config.

#### Scenario: Nearby view

- **WHEN** the menu opens on the Nearby view
- **THEN** it lists only players in the local location within the reach declared by each of those players

#### Scenario: Different voice reaches

- **WHEN** another player has a 20-tile reach and the local player has a 3-tile reach
- **THEN** the other player appears in Nearby while within their own 20-tile reach

#### Scenario: Toggle from the menu

- **WHEN** the player selects a nearby row
- **THEN** that player is muted or unmuted, the row reflects the new state and the config is saved immediately

#### Scenario: Muted player remains listed

- **WHEN** a nearby player is muted
- **THEN** they remain in the Nearby view with a visible muted state so they can be unmuted

### Requirement: All players view

The menu SHALL provide an All view listing every current player in the server except the local player, regardless of game location or distance. A player muted from the All view SHALL remain muted when they later appear in the Nearby view or speak.

#### Scenario: Player elsewhere on the map

- **WHEN** another player is in a different location
- **THEN** they do not appear in Nearby but they do appear in All

#### Scenario: Mute from All

- **WHEN** the local player mutes a player from the All view
- **THEN** that player is silent locally whenever they speak, regardless of distance

#### Scenario: Player list changes

- **WHEN** a player joins or leaves the server
- **THEN** the menu reflects the current player list the next time it is opened or refreshed

### Requirement: Muted nearby players remain visible in the HUD

The mod SHALL show a muted player in the local game location and within the voice reach used by the indicator, with a visible muted-microphone marker, even when they are not currently speaking. Muting SHALL NOT remove the player from the nearby list while they are in range. Unmuted players SHALL remain listed only while speaking.

#### Scenario: Muted player is nearby and silent

- **WHEN** a nearby player is muted and not speaking
- **THEN** the HUD lists that player with a muted-microphone marker

#### Scenario: Muted player speaks nearby

- **WHEN** a nearby player is muted and speaks
- **THEN** the HUD lists that player with a muted-microphone marker

#### Scenario: Muted player is out of range

- **WHEN** a muted player is outside the voice reach used by the indicator
- **THEN** they are not listed in the nearby HUD

### Requirement: Local-only and non-interfering

Mute state SHALL be local. It SHALL NOT be transmitted to other players, SHALL NOT change the voice protocol and SHALL NOT prevent a muted player from hearing the local player. The player-mute menu SHALL NOT change the existing HUD's click-through behavior, and SHALL NOT open in single-player.

#### Scenario: Muted player still hears the local player

- **WHEN** the local player mutes another player
- **THEN** the muted player can still hear the local player unless they have muted the local player in turn

#### Scenario: Other clients are unaffected

- **WHEN** one client mutes a player
- **THEN** other clients continue to hear that player normally

#### Scenario: Single-player

- **WHEN** the player presses the menu keybind in a single-player save
- **THEN** no player-mute menu opens
