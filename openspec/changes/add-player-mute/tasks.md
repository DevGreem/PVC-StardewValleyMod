# Tasks

## 1. Mute data and configuration

- [x] 1.1 Add `MutedPlayer` entries, `MutedPlayers` and `PlayerMuteKey` (default `F10`) to `ModConfig`; verify defaults, serialization round-trip, old configs loading an empty list and reset clearing the list/restoring the key
- [x] 1.2 Add a `PlayerMuteList` service with ID-based lookup, add/remove/toggle and normalization; verify unit tests cover duplicates, invalid IDs, toggle behavior and reset
- [x] 1.3 Normalize the saved mute list on load (trim names, drop invalid IDs and deduplicate); verify malformed or legacy configs still start and preserve valid entries

## 2. Player-mute menu

- [x] 2.1 Add a pure menu model that builds Nearby and All rows from `Game1.otherFarmers`, filtering Nearby by same location and the local voice reach; verify tests cover other locations, distance, sorting and mute state
- [x] 2.2 Implement `PlayerMuteMenu : IClickableMenu` with Nearby/All views, player rows, mute toggles, close behavior and immediate `SaveConfig`; verify the menu lists current players and reflects state changes
- [x] 2.3 Wire `PlayerMuteKey` in `OnButtonPressed`: open the menu only in multiplayer, suppress the key and do not open over another menu; verify single-player and keybind behavior
- [x] 2.4 Show muted nearby speakers in the HUD with a muted-microphone marker while keeping the HUD click-through; verify a muted speaking player remains visible and an out-of-range one does not

## 3. Playback integration

- [x] 3.1 Keep receiving and decoding muted players so their speaking state remains available, discard their decoded buffers and force their channel volume to zero in `UpdateSpeakerState`; verify a muted player is silent and an unmuted player becomes audible again
- [x] 3.2 Rebuild the runtime mute set when the config changes and keep the menu and playback in sync; verify mute/unmute from the menu applies immediately
- [x] 3.3 Track whether each speaker is nearby independently of volume, so the HUD can list muted in-range speakers; verify the nearby state follows location and voice reach

## 4. GMCM and documentation

- [x] 4.1 Extend `IGenericModConfigMenuApi` with `AddParagraph`, expose the player-mute keybind and a paragraph explaining the local menu; verify the panel lists both
- [x] 4.2 Update `README.md` with the menu key, Nearby/All behavior, local-only mute semantics, persistence and reset clearing the list; verify documented defaults match the code

## 5. Integration verification

- [x] 5.1 Run `dotnet test PVC.slnx` and `dotnet build PVC.slnx`; verify all tests pass and the build has no warnings or errors
- [x] 5.2 In a two-player world, mute and unmute from Nearby and from All; verify only the local listener is affected and the other player still hears normally
- [x] 5.3 Restart the game and verify the saved mutes persist; verify the existing HUD remains click-through and the menu key does nothing in single-player
- [ ] 5.4 Mute a nearby speaking player and verify they remain visible in the HUD with the muted-microphone icon; verify they disappear only when out of range

## Workflow follow-up

- Archive after the remaining HUD muted-icon check is complete.
- Clickable HUD mute rows are a possible future enhancement; this change deliberately uses the menu as the interactive surface.
