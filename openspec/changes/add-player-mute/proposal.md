# Proposal

## Why

Players can currently only control the master playback volume. There is no way to silence one specific farmer, and the HUD only lists nearby speakers, so a player cannot manage someone who is elsewhere on the map. A local per-player mute list, with a nearby view and a full-server view, gives players direct control without changing what anyone else hears.

## What Changes

- Add a **local per-player mute list**. Muting a player suppresses that player's voice only on the local machine; it does not mute the remote microphone for other players.
- Persist the mute list in the local player's `config.json`, keyed by `UniqueMultiplayerID` and keeping the last known name for display.
- Add a dedicated **player-mute menu**, opened by a configurable keybind (default `F10`):
  - **Nearby** view: players in the same game location within the local player's active voice reach.
  - **All** view: every current player in the server, regardless of location or distance.
  - Each row shows the player name, current location, and mute state; clicking toggles the mute and saves the config immediately.
- Keep the existing HUD indicator purely visual and click-through. The menu is the interactive surface; this avoids intercepting normal game clicks.
- Keep muted nearby players visible in the HUD list with a muted-microphone marker, even when they are silent, so muting someone does not hide that they are nearby. Muted packets are still decoded enough to know whether the player is speaking, but their volume stays at zero.
- Expose the menu keybind and a short explanatory paragraph in Generic Mod Config Menu. GMCM does not get a dynamic player list because its current API subset only supports static options.
- Add the mute keybind to the keybind settings and document that resetting settings clears the local mute list.
- No voice packet, protocol or network change. A muted player can still hear the local player unless that player mutes them in turn.

## Capabilities

### New Capabilities

- `player-mute`: local per-player mute state, persistence, playback suppression and the Nearby/All player menu.

### Modified Capabilities

- `voice-chat-settings`: adds the player-mute menu keybind and GMCM paragraph, and clarifies that reset clears the local mute list.

## Impact

- **Code**: `Config/ModConfig.cs`, `Config/ConfigManager.cs` normalization, a mute-list service, `ModEntry` keybind/input/volume wiring, a new `UI/PlayerMuteMenu.cs`, `Integrations/GmcmIntegration.cs` and `IGenericModConfigMenuApi.cs`, tests and `README.md`.
- **Settings**: one new keybind (`PlayerMuteKey`, default `F10`) and a persisted `MutedPlayers` list. Existing configs remain valid with an empty list.
- **UI**: a custom `IClickableMenu` with Nearby/All views; the existing HUD now marks muted nearby speakers but remains visual and click-through.
- **Network**: none; mute state is local and is never transmitted.
- **Risks**: `UniqueMultiplayerID` stability across sessions, stale saved entries for absent players, keybind collisions, and players expecting the mute to affect the remote microphone rather than only their own playback.
