# Design

## Context

See `proposal.md` for motivation. `ModConstants.ProximityRadii` currently defines `3/10/20`, with `10` as the default. `ModConfig.Normalize` maps legacy `6` and `15` values to the default, and `PacketValidator` accepts only radii from the current offered set. The sender encodes its configured radius in every voice packet; the HUD and mute menu already read the radius from that packet or use the maximum offered value as a fallback.

## Goals / Non-Goals

**Goals:**

- Offer `3`, `15` and `30` tiles with `15` as the default.
- Preserve existing configs through deterministic migration.
- Let updated clients keep hearing old clients that still send `6/10/20`.
- Keep all radius-dependent UI and fallback logic driven by the shared radius list.

**Non-Goals:**

- Changing the voice-packet wire format or adding a version negotiation.
- Making old clients accept the new `30` value.
- Changing per-speaker volume falloff or the local listener's own reach semantics.
- Adding arbitrary user-entered radii.

## Decisions

### D1 — Use `3`, `15`, `30`, with `15` as the default

The three values keep a private/short option, a normal conversational option and a genuinely long option. `15` is the closest replacement for the old default `10` while still increasing the normal range.

- **Alternative rejected:** keeping `10` as the default and only adding `30`, which would leave the normal option unchanged and make the spread uneven.
- **Alternative rejected:** offering four values, which makes cycling and the documented choice less clear.

### D2 — Normalize legacy configs by nearest intended tier

`ModConfig.Normalize` maps `6 -> 3`, `10 -> 15`, `20 -> 30`; `3` and `15` stay unchanged. Any other value still falls back to `15`.

- **Why:** `6` was a short-range option, `10` the old default/normal option and `20` the old long option; mapping each to its intended tier preserves the player's choice better than collapsing everything to the default.
- **Alternative rejected:** mapping every removed value to `15`, which would unexpectedly turn short-range users into normal-range users.
- **Consequence:** existing users may hear and be heard at a different distance after updating; the README and migration note call this out.

### D3 — Accept legacy radii inbound, advertise current radii outbound

`PacketValidator` accepts the current set plus the legacy `6/10/20` values. The sender still uses only the configured current value, so new clients never advertise a legacy radius. This lets updated clients hear peers that have not changed their config or are still on the old build.

- **Why:** accepting legacy packet values is backward-compatible and costs only a small validation branch.
- **Alternative rejected:** rejecting legacy values would silence old peers immediately after one side updates.
- **BREAKING:** old clients still validate only `3/10/20`, so they reject packets carrying `30`. Mixed-version sessions cannot use the new long range until every client updates.
- **Why not bump the wire version:** an old client would reject the new version anyway, while a version bump would also discard compatible old `3/10/20` packets. Keeping version 1 maximizes partial compatibility.

### D4 — Drive UI fallbacks from the shared list

The HUD and player-mute menu use the last declared packet radius when known and the maximum entry in `ModConstants.ProximityRadii` otherwise. Changing the array therefore changes the fallback from `20` to `30` without separate UI constants.

- **Alternative rejected:** hardcoding `30` in the HUD/menu, which would drift from the offered set later.

## Risks / Trade-offs

- **[Mixed versions cannot use 30]** → Document that all players should update together; old `3/10/20` speakers remain audible to updated clients.
- **[Users' effective range changes after migration]** → Log normalization and document the mapping; reset-to-defaults uses `15`.
- **[Tests hardcode `3/10/20`]** → Update radius-set, cycle, normalization, validator and README tests together; keep proximity math tests parameterized where possible.
- **[A future fourth radius changes the fallback]** → The shared-array decision keeps the fallback correct automatically.

## Migration Plan

1. Change the offered set/default and normalization mapping.
2. Update packet validation to accept current and legacy values.
3. Update GMCM/README and tests.
4. Deploy; existing configs normalize on the next load.
5. Rollback is safe: the old build normalizes `15` and `30` to its default `10`; no new files or wire format are introduced.
