# Design

## Context

See `proposal.md` for motivation. `ModEntry.OnRenderedHud` already builds a distance-sorted list of eligible nearby players, and `VoiceIndicator` draws it. The HUD currently caps the array at eight players with no indication that more exist. The player-mute menu has its own eight-row cap and is independent of this list.

## Goals / Non-Goals

**Goals:**

- Keep the HUD compact with at most eight nearby rows: seven players plus one overflow summary.
- Report the total number of additional nearby players with correct singular/plural wording.
- Preserve the existing nearby eligibility, distance ordering, mute markers and click-through behavior.

**Non-Goals:**

- Changing the player-mute menu or its `All` view.
- Adding scrolling, pagination or interactive HUD rows.
- Changing how proximity or voice reach is calculated.
- Showing muted vs unmuted status in the summary row.

## Decisions

### D1 — Seven players plus one summary row

The HUD keeps the existing eight-row maximum by showing seven player rows and using the eighth row for the overflow summary when needed. This preserves the panel height and avoids pushing the HUD further down the screen.

- **Alternative rejected:** showing eight players plus a summary, which grows the panel to nine rows.
- **Alternative rejected:** scrolling or pagination, which would make the HUD interactive or require additional input handling.

### D2 — Count every eligible player beyond the visible seven

After sorting by distance, `ModEntry` takes the first seven players and computes `hidden = totalEligible - visible`. The summary reports that total, so nine players produce `+2 players nearby` and twelve produce `+5 players nearby`.

- **Alternative rejected:** reporting only the eighth player and ignoring the rest, which would understate how crowded the area is.
- **Consequence:** the summary row can represent players who are not individually identifiable from the HUD; the mute menu remains the place to inspect the full list.

### D3 — Format the summary in a pure rule

Add an `IndicatorRules` formatter that returns `+1 player nearby` for one hidden player and `+N players nearby` for more. `VoiceIndicator` receives the hidden count and draws the row in the same secondary color used for auxiliary HUD text.

- **Why:** singular/plural handling is easy to unit test without constructing a SpriteBatch or game state.
- **Alternative rejected:** hardcoding the string in `VoiceIndicator`, which would leave the plural edge case untested.

### D4 — Leave the player-mute menu unchanged

The `Nearby`/`All` menu keeps its own eight-row cap and existing filtering. This change only affects the HUD's summary; it does not add a shared list component or refactor the menu.

## Risks / Trade-offs

- **Hidden players cannot be individually identified from the HUD** → The summary communicates the count and the mute menu exposes the full nearby list.
- **The seven-player threshold feels arbitrary** → It is chosen to preserve the current eight-row HUD height; the summary row accounts for the difference.
- **More than eight nearby players in a crowded session** → The HUD still draws a bounded number of rows; no new per-frame allocation beyond the existing list work.
