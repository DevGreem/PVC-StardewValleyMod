# Proposal

## Why

The HUD currently stops at eight nearby players, so in a crowded session the player cannot tell whether anyone else is nearby or how many. The list should stay compact while still showing the total nearby count.

## What Changes

- Keep the current HUD layout and click-through behavior.
- When seven or fewer players are nearby, list all of them as today.
- When eight or more are nearby, show the seven nearest players and add one summary row:
  - `+1 player nearby` for eight nearby players;
  - `+2 players nearby` for nine;
  - and so on for `total - 7` hidden players.
- The summary counts every nearby player beyond the seven visible rows, not just the eighth.
- No change to the player-mute menu's `Nearby`/`All` rows or to its eight-row display cap.
- No change to how nearby players are selected; the existing distance/location/mute rules still decide the set and its order.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `voice-activity-indicator`: the nearby list gains a seven-player display cap and an overflow summary row that reports the number of additional nearby players.

## Impact

- **Code**: `ModEntry` computes the visible/hidden split, `VoiceIndicator` renders the summary row, and `IndicatorRules` owns the singular/plural summary text.
- **UI**: one extra HUD row only when eight or more players are nearby; panel height stays at the current eight-row maximum.
- **Network/config**: none.
- **Tests**: pure rule tests for overflow formatting and the seven/seven-plus-one split; existing indicator tests remain valid.
