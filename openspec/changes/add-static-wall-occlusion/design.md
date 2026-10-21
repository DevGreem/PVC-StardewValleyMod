# Design

## Context

See `proposal.md` for motivation. Today `ModEntry.UpdateSpeakerState` computes one gain from distance and the speaker's declared voice reach, then `PlaybackMixer` sums each `SpeakerChannel` with that gain. `SpeakerChannel.TryReadFrame` already runs on the audio thread and owns per-speaker decoder/jitter state, so it is the natural place for a per-speaker filter.

The reference mod `dailyxcz/StardewValleyMods` (`VoiceChat`) already implements a similar feature: it walks the line between two farmers over the `Buildings` layer, skips passable tiles, and applies a one-pole low-pass filter based on the number of blocked tiles. This design reuses those proven choices and adds gain attenuation and a silence threshold.

## Goals / Non-Goals

**Goals:**

- Make static Stardew Valley walls audible as obstacles without a network protocol change.
- Produce the three requested outcomes: quieter, muffled and silent.
- Keep the calculation local, allocation-free on the audio thread and cheap enough to run every tick.
- Give the host a single authoritative toggle so every player hears a coherent occlusion model.
- Fall back to occlusion enabled when the host does not run the mod, without allowing clients to change it.

**Non-Goals:**

- Player-placed objects, trees, fences, furniture or other dynamic blocks; they are tracked for a future change.
- Per-material or per-object occlusion levels.
- Per-client overrides of the host's occlusion setting.
- `Back` or `Front` map layers, water, cliffs, windows or line-of-sight through floors.
- Stereo panning, reverberation or a full acoustic model.
- Any packet, version or transport change.

## Decisions

### D1 — Use the `Buildings` layer and constructed building footprints

`CountBlockingTiles` counts two sources: solid tiles in `location.Map.GetLayer("Buildings")`, and tiles covered by each building in `location.buildings` (`Building.occupiesTile(x, y, applyTilePropertyRadius: false)`). The first covers static house walls, building shells and closed doors; the second covers cabins, sheds, barns and coops, which are `Building` objects rather than map tiles. The manual cabin test showed why both are needed: a cabin between two farmers was not in the map layer, so it did not block.

- **Why:** it matches the visible static geometry the player expects to block sound, including player-constructed buildings.
- **Alternative rejected:** only the `Buildings` layer misses cabins, sheds, barns and coops.
- **Alternative rejected:** `GameLocation.isTilePassable` also sees objects, terrain and characters, which would mix dynamic blocks into this change.
- **Alternative rejected:** the `Back` layer includes floors, water and cliffs; counting it would create false positives and is deferred.
- **Consequence:** a building footprint can contribute many tiles, so a cabin or shed between two farmers can reach the silence threshold. That is the intended behavior for a solid building.

### D2 — Walk the line with 2× oversampling

Sample the segment from the speaker tile to the listener tile at two samples per tile, skip the endpoints, round to tile coordinates and ignore consecutive duplicate tiles. A tile blocks when it is inside a constructed building footprint or when it is a non-null map tile that is not marked passable. A `Passable` property with a true value (`T`/`true`) means an open door and does not block; a false value or no property means a solid tile.

- **Why:** two samples per tile avoid skipping a one-tile diagonal wall, and the endpoint skip avoids counting the tile a farmer stands on.
- **Alternative rejected:** a full supercover/Bresenham implementation is more code for a line that is at most the voice reach (20 tiles).
- **Consequence:** a thick wall naturally contributes more than one blocker, which is desirable.

### D3 — One blocker is quieter, two are heavily muffled, three or more are silent

Use a pure occlusion helper:

- `Gain = max(0, 1 - 0.4 × blockedTiles)`: 0 blockers = 1.0, 1 = 0.6, 2 = 0.2, 3+ = 0.0.
- `LowPassAlpha = max(0.08, 1 - 0.45 × blockedTiles)`: 0 blockers = 1.0 (passthrough), 1 = 0.55, 2 = 0.10, 3+ = 0.08 (irrelevant when gain is zero).

- **Why:** it produces the requested progression and is simple to test as pure math.
- **Alternative rejected:** the reference's low-pass-only model never reaches silence and has no volume attenuation.
- **Alternative rejected:** a binary wall check is too crude and makes every fence-sized obstacle an all-or-nothing event.

### D4 — Apply the low-pass per `SpeakerChannel`

`SpeakerChannel` gains an occlusion low-pass target set by the game thread. `TryReadFrame` slews the current alpha toward that target (about 0.1 per 20 ms frame), filters the dequeued frame in place and resets the filter state in `Clear()`. `Volume` is set to `distanceGain × occlusionGain`; `PlaybackMixer` does not need to change.

- **Why:** the channel already owns mutable per-speaker audio state and runs on the audio thread, so no shared filter table or lock is added.
- **Alternative rejected:** filtering in `PlaybackMixer` would require separate filter state per source and would mix concerns.
- **Consequence:** the slew avoids clicks when a player walks behind a wall; already-buffered frames use the previous alpha until replaced, bounded by the jitter buffer.

### D5 — Compute occlusion in `UpdateSpeakerState` on the game thread

For each speaker that is already in range, compute the blocked-tile count from `farmer.Tile` to `Game1.player.Tile`, then set the channel's gain and low-pass target. Recompute every tick so walking, doors and map changes take effect without packets.

- **Why:** map access belongs on the game thread; packets are not the right trigger because they do not change when a wall or door changes.
- **Alternative rejected:** computing on packet receipt would leave stale occlusion while a player stands still.
- **Performance:** at most 8 mixed speakers × roughly 40 samples per line per tick; far speakers are skipped before the walk.

### D6 — `WallOcclusion` is a host-authoritative setting

`WallOcclusion` is persisted as the host's preference. Only `Context.IsMainPlayer` can change it. The mod keeps a separate `effective` value used by playback: the host uses its persisted preference; a client starts at `true` and applies the host's value when it arrives. A client's local config value is ignored during the session, and a client attempt to change the setting is rejected.

- **Why:** every player must hear a coherent occlusion model, and the host is the only player all clients can agree on.
- **Alternative rejected:** local per-player control lets one listener hear through walls while another does not, which was the original ambiguous behavior.
- **Fallback:** if the host does not run PVC, no host value arrives; clients keep `true` and cannot change it.
- **Consequence:** the GMCM control is effectively read-only for clients; the tooltip explains that the host owns it.

### D7 — Synchronize the host value with a dedicated SMAPI message

Add `WallOcclusionMessage { bool Enabled }` and a `wall-occlusion` message type. The host sends it:

- after a save loads, when the host is the main player;
- when a peer connects, targeted at that peer;
- when the host changes the value.

A client applies the message only when all of these hold: it is not the main player, the sender is `Game1.MasterPlayer`, the mod ID matches, and the type matches. Other clients cannot spoof another sender's player ID through SMAPI. The host ignores settings messages from clients.

- **Why:** a dedicated message is smaller and easier to validate than adding a field to every voice packet, and the voice wire format stays unchanged.
- **Alternative rejected:** putting the boolean in each voice packet wastes bandwidth and mixes a session setting with media.
- **Late join:** `PeerConnected` re-sends the current value, so a client that joins after the initial broadcast is brought up to date. A client also sends a request when its save finishes loading; the host answers that player directly, so a message sent while the client was still loading is not lost.

### D8 — Tests are split between pure math, line walking and sync policy

- `OcclusionMath` tests cover 0/1/2/3+ blockers, monotonic gain, alpha floor and silence.
- A line-walk test uses a small fake grid to cover clear lines, one wall, open doors and diagonal walls.
- A building-footprint test simulates a multi-tile cabin/shed footprint and verifies it contributes enough blockers to reach the silence threshold.
- `SpeakerChannel` tests cover filter state, gain composition and reset.
- Sync-policy tests cover host-only acceptance, client fallback to `true` without a host value, and rejection of non-host senders.
- Config tests cover default true, reset and old-config load.
- Manual two-player checks cover a thin wall, a thick wall/building interior, the host toggle and the client's inability to override it.

## Risks / Trade-offs

- **A client expects a local escape hatch** → The setting is intentionally read-only for clients; the tooltip and README explain that the host owns it.
- **Host does not run PVC** → No host message arrives; clients keep occlusion enabled and cannot change it.
- **Host changes the value while a client is joining** → The host sends the current value on `PeerConnected`; the latest host message wins.
- **A malicious client sends a settings message** → Only messages from `Game1.MasterPlayer` with the PVC mod ID and message type are accepted.
- **Decorative `Buildings` tiles count as blockers** → The layer is the reference's proven choice; `Passable` excludes open doors. If a specific map decoration causes false positives, a later change can add tile-property filtering.
- **A single thick wall can reach the three-tile silence threshold** → This is realistic for a solid building wall, but it is the most likely tuning complaint. The constants are isolated in `OcclusionMath` and the host can disable the feature.
- **Low-pass dulls speech** → Use a mild alpha for one wall and let the host disable it; tune after listening tests without changing the spec.
- **Constructed building footprints include the whole building, including the door tile** → This is correct for a solid cabin/shed seen from outside; a future change can treat open doorways as pass-through if needed.
- **Modded maps may lack a `Buildings` layer or use custom tiles** → Missing layer still allows constructed building footprints to count; custom tiles still use the `Passable` property. This is a safe degradation.
- **Dynamic objects remain inaudible through** → Explicit non-goal; the future SP task covers trees, fences and per-object levels.
- **Game-thread cost** → Only in-range speakers are walked, lines are capped by the voice reach and the walk is allocation-free.
- **Audio-thread cost** → One one-pole filter per active channel; negligible compared with Opus decode and mixing.

## Migration Plan

- Add the `WallOcclusion` field with a default of `true`; older config files load it as `true`.
- The field is the host's preference. Clients keep their local field for when they host, but ignore it while connected to another host.
- No voice wire-format change; the new settings message is additive.
- Rollback: the host sets `WallOcclusion` to `false` or removes the field; old builds ignore the message type and the unknown JSON field.

## Open Questions

None. The exact attenuation constants can be tuned after listening tests; the one/two/three-blocker behavior is fixed by the spec.
