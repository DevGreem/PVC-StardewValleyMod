# Design

## Context

See `proposal.md` for motivation. Today `ModEntry.UpdateSpeakerState` computes one gain from distance and the speaker's declared voice reach, then `PlaybackMixer` sums each `SpeakerChannel` with that gain. `SpeakerChannel.TryReadFrame` already runs on the audio thread and owns per-speaker decoder/jitter state, so it is the natural place for a per-speaker filter.

The reference mod `dailyxcz/StardewValleyMods` (`VoiceChat`) already implements a similar feature: it walks the line between two farmers over the `Buildings` layer, skips passable tiles, and applies a one-pole low-pass filter based on the number of blocked tiles. This design reuses those proven choices and adds gain attenuation and a silence threshold.

## Goals / Non-Goals

**Goals:**

- Make static Stardew Valley walls audible as obstacles without a network protocol change.
- Produce the three requested outcomes: quieter, muffled and silent.
- Keep the calculation local, allocation-free on the audio thread and cheap enough to run every tick.
- Give players a local toggle so the feature can be compared or disabled.

**Non-Goals:**

- Player-placed objects, trees, fences, furniture or other dynamic blocks; they are tracked for a future change.
- Per-material or per-object occlusion levels.
- `Back` or `Front` map layers, water, cliffs, windows or line-of-sight through floors.
- Stereo panning, reverberation or a full acoustic model.
- Any packet, version or transport change.

## Decisions

### D1 — Use the `Buildings` layer as the static-wall source

`CountBlockingTiles` reads `location.Map.GetLayer("Buildings")`. Static house walls, building shells and closed doors live there; floor decorations and terrain do not. Dynamic trees, fences and furniture live in the game's object/terrain collections, which keeps them out of this MVP automatically.

- **Why:** it matches the visible static geometry the player expects to block sound, and it is the same layer choice as the reference implementation.
- **Alternative rejected:** `GameLocation.isTilePassable` also sees objects, terrain and characters, which would mix dynamic blocks into this change.
- **Alternative rejected:** the `Back` layer includes floors, water and cliffs; counting it would create false positives and is deferred.

### D2 — Walk the line with 2× oversampling

Sample the segment from the speaker tile to the listener tile at two samples per tile, skip the endpoints, round to tile coordinates and ignore consecutive duplicate tiles. A tile blocks when it is non-null and is not marked passable. A `Passable` property with a true value (`T`/`true`) means an open door and does not block; a false value or no property means a solid tile.

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

### D6 — `WallOcclusion` is a local, live, default-on setting

Add `WallOcclusion` (boolean, default `true`) to `ModConfig`, `ResetToDefaults`, GMCM and the README. It is read by `UpdateSpeakerState`; toggling it applies on the next tick. It is never sent to other players.

- **Why:** it lets players compare the effect and opt out, and it makes manual testing possible without a rebuild.
- **Alternative rejected:** always-on has no escape hatch if a map produces false positives.
- **Consequence:** two players in the same session can hear the same speaker differently if one disables occlusion; that is intentional and local.

### D7 — Tests are split between pure math and map-independent line walking

- `OcclusionMath` tests cover 0/1/2/3+ blockers, monotonic gain, alpha floor and silence.
- A line-walk test uses a small fake grid to cover clear lines, one wall, open doors and diagonal walls.
- `SpeakerChannel` tests cover filter state, gain composition and reset.
- Config tests cover default true, reset and old-config load.
- Manual two-player checks cover a thin wall, a thick wall/building interior and the toggle.

## Risks / Trade-offs

- **Decorative `Buildings` tiles count as blockers** → The layer is the reference's proven choice; `Passable` excludes open doors. If a specific map decoration causes false positives, a later change can add tile-property filtering.
- **A single thick wall can reach the three-tile silence threshold** → This is realistic for a solid building wall, but it is the most likely tuning complaint. The constants are isolated in `OcclusionMath` and the toggle disables the feature.
- **Low-pass dulls speech** → Use a mild alpha for one wall and the toggle; tune after listening tests without changing the spec.
- **Modded maps may lack a `Buildings` layer or use custom tiles** → Missing layer means no occlusion; custom tiles still use the `Passable` property. This is a safe degradation.
- **Dynamic objects remain inaudible through** → Explicit non-goal; the future SP task covers trees, fences and per-object levels.
- **Game-thread cost** → Only in-range speakers are walked, lines are capped by the voice reach and the walk is allocation-free.
- **Audio-thread cost** → One one-pole filter per active channel; negligible compared with Opus decode and mixing.

## Migration Plan

- Add the `WallOcclusion` field with a default of `true`; older config files load it as `true`.
- No wire-format change and no data migration.
- Rollback: set `WallOcclusion` to `false` or remove the field; old builds ignore an unknown JSON field and keep working.

## Open Questions

None. The exact attenuation constants can be tuned after listening tests; the one/two/three-blocker behavior is fixed by the spec.
