# Design

## Context

See `proposal.md` for motivation. `ModEntry` owns the current speakers in `_speakers` and applies distance/occlusion gain in `UpdateSpeakerState`. `VoiceIndicator` is deliberately a draw-only, click-through HUD. `Game1.otherFarmers` already gives the current server players and their locations, and the config is a plain JSON object reloaded at startup.

The current GMCM subset only supports static options (`AddBoolOption`, `AddNumberOption`, `AddTextOption`, `AddKeybind`, section titles). It cannot build a dynamic player list. A dedicated `IClickableMenu` is the reliable place for the Nearby/All views.

## Goals / Non-Goals

**Goals:**

- Silence a specific remote player locally, immediately and persistently.
- Let the player manage both nearby farmers and anyone else in the server.
- Keep the existing HUD visual and click-through, while keeping muted nearby players listed and marked.
- Keep the feature local: no protocol, packet or host-authority changes.

**Non-Goals:**

- Muting another player's microphone for everyone.
- Host-enforced mute lists or server moderation.
- Clickable rows in the HUD (the menu is the interactive surface).
- A dynamic player list inside GMCM.
- Cross-device or cross-save synchronization of the mute list.
- A separate mute-all/unmute-all action.

## Decisions

### D1 — Persist a list of `(UniqueMultiplayerID, name)` entries

Add `MutedPlayers` to `ModConfig` as a list of small entries containing the long `UniqueMultiplayerID` and the last known `PlayerName`. A `PlayerMuteList` runtime service builds a `HashSet<long>` for constant-time lookup and exposes add/remove/toggle/contains operations.

- **Why:** the ID is the stable lookup key; the name is only for display. A set makes the per-tick playback check cheap.
- **Alternative rejected:** matching only by name breaks with duplicate or changed names.
- **Normalization:** trim names, drop invalid/non-positive IDs and deduplicate by ID. The list survives restarts; reset-to-defaults clears it.

### D2 — Keep decoding muted players so the HUD can show their speaking state

If a player is muted, `UpdateSpeakerState` forces the channel volume to zero and removes it from the mixer snapshot. The channel still receives and decodes packets with `enqueueAudio: false`, because `IsSpeaking` is what feeds the HUD's nearby list; decoded frames are discarded instead of buffered. Any audio queued before the mute is also dropped. When the player is unmuted, the next packet starts a fresh buffer and the channel becomes audible again.

- **Why:** dropping the packets before decoding would make a muted speaker disappear from the HUD, which is exactly what this enhancement must avoid.
- **Alternative rejected:** muting at receive time saves CPU but loses the speaking state needed by the indicator.
- **Consequence:** muted speakers cost decode work while they are in range; the concurrent-speaker cap still bounds the mixer, not the decoder. A muted channel is also kept through the stale-packet sweep, so the HUD retains the last declared reach while the farmer is still connected; the channel is disposed on disconnect or after unmuting and going idle.

### D3 — Use a dedicated `PlayerMuteMenu` with Nearby and All views

Add `PlayerMuteMenu : IClickableMenu`, opened by `PlayerMuteKey` (default `F10`). It has two views:

- **Nearby:** `Game1.otherFarmers` in the same location and within the voice reach declared by each other player. The local player's outgoing reach controls only how far their own voice carries, so it must not filter who can be muted. The declared reach is learned from accepted voice packets and remembered for the session; until it is known, the maximum offered reach (20 tiles) is used.
- **All:** every current `Game1.otherFarmers` entry except the local player, with location, own reach and mute state.

Each row shows the name, location, own voice reach and a clear muted/unmuted state. Clicking a row toggles the mute and calls `SaveConfig` immediately. The menu rebuilds its rows when opened and when the player list changes; it does not open in single-player or on top of another menu.

- **Why:** one menu satisfies both requests without making the HUD capture game clicks, and using the speaker's reach matches how incoming voice actually works.
- **Alternative rejected:** filtering by the local player's reach hides distant-but-audible players and forces a trip to All.
- **Alternative rejected:** GMCM cannot render a dynamic list with the current API subset.
- **Alternative rejected:** clickable HUD rows would require hitboxes, mouse suppression and careful interaction with the game's UI; the menu is safer and more discoverable.

### D4 — Add the keybind and explanation to GMCM

Extend `IGenericModConfigMenuApi` with `AddParagraph` (GMCM supports it) and register:

- a `Player-mute menu key` keybind;
- a paragraph explaining that mutes are local, saved and opened with that key.

GMCM does not list players; the paragraph points to the menu.

- **Why:** the key must be discoverable and configurable, but the dynamic content belongs in the menu.
- **Alternative rejected:** a text option with player names would be awkward, limited to one selection and not refresh reliably.

### D5 — Keep the HUD visual, click-through and informative

`VoiceIndicator` continues to draw only the microphone state, voice reach and nearby players. It does not add hitboxes or capture clicks. Muted players remain in the list while they are in range, even when they are silent; if no recent voice channel exists, the row comes from the live farmer data, using at least the default 10-tile reach because the speaker's declared reach is unknown. Unmuted players are still listed only while speaking. Each muted row gains a muted-microphone marker scaled to the font line height; the marker is drawn with primitives, so no texture asset is required. The menu remains the only interactive mute surface.

- **Why:** the existing indicator spec explicitly requires click-through; adding clickable rows would complicate that contract, but the player still needs to see who they have muted nearby.
- **Alternative rejected:** hiding muted players, which makes it look like they are not there.
- **Alternative rejected:** showing every unmuted player all the time, which would flood the HUD with silent players.
- **Alternative rejected:** a togglable interactive HUD overlay; defer it to a future change if players want it.

### D6 — Defaults and reset

- `PlayerMuteKey` defaults to `F10` (outside the existing `V`, `F7`, `F8`, `F9` bindings).
- `MutedPlayers` starts empty; older config files load an empty list.
- `ResetToDefaults` clears the mute list and restores the key to `F10`.

- **Why:** reset should return the mod to a clean, unmuted state, and the list is part of the persisted config.
- **Consequence:** players who reset their settings lose their saved mutes; the README and menu should make this clear.

### D7 — Tests

- `PlayerMuteList` tests: add, remove, toggle, duplicate handling, lookup by ID and reset.
- `ModConfig` tests: default empty list, serialization round-trip, normalization of duplicates/invalid IDs and reset.
- Menu-model tests: Nearby filters by location and each player's declared reach; All includes players in other locations; rows are sorted and carry the reach and mute state.
- Indicator tests: a nearby muted player is considered for the HUD even when silent, while an out-of-range muted player is not.
- Manual checks: mute/unmute from Nearby, mute someone from All, restart persistence, muted player still hears the local player, the muted nearby player remains visible with the icon even while silent, and the HUD remains click-through.

## Risks / Trade-offs

- **`UniqueMultiplayerID` changes between sessions** → The name is kept for display but the ID is authoritative; a changed ID means the entry no longer applies. The menu shows the current players so the player can re-mute.
- **Stale entries for absent players** → Entries are retained so a player can be muted across sessions; they are not shown in All until that player is online. Reset clears them.
- **Keybind collision** → The default F10 is outside the other PVC defaults; the key is configurable and the existing key parsing uses the game's key names.
- **Menu input conflicts with the game or another menu** → Do not open when another `IClickableMenu` is active; the menu owns its clicks while open.
- **Players expect a remote mic mute** → The menu labels the action as local mute and the README explains that other players still hear the muted farmer.
- **Reset clears the list unexpectedly** → Document the behavior in the menu/README and test it.

## Migration Plan

- Add `PlayerMuteKey` (`F10`) and `MutedPlayers` (empty) to the config; older files load them from the property defaults.
- No network or protocol migration.
- Rollback: remove the menu/keybind and the list; older builds ignore the unknown JSON fields and keep working.
