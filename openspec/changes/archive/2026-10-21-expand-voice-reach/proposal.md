# Proposal

## Why

The current reach values force players to stay very close for quiet conversations and top out at 20 tiles. A wider spread gives players a clearer short/normal/long choice and lets groups hear each other across larger rooms or farm areas without changing the local listener's settings.

## What Changes

- Change the offered outgoing voice reaches from `3`, `10`, `20` to `3`, `15`, `30` tiles.
- Make `15` tiles the documented default.
- Normalize existing configs on load:
  - `3` remains `3`;
  - `6` becomes `3`;
  - `10` becomes `15`;
  - `15` remains `15`;
  - `20` becomes `30`.
- Keep accepting the legacy `6`, `10` and `20` values in incoming voice packets so updated clients can still hear peers that have not updated their config or are still sending legacy reaches.
- **BREAKING** for mixed mod versions: clients still running the old build reject packets that declare `30`, so all players in a session must update together to use the new long range. Updated clients continue to accept the legacy reaches from old speakers.
- Update the HUD/mute-menu fallback to the maximum offered reach (now `30`) through the shared radius list.
- Update GMCM labels, README, tests and the fixed-set specification.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `voice-chat-settings`: the offered radius set and default change to `3/15/30`, with explicit migration of old values.
- `voice-transport`: incoming packet validation accepts the current set plus legacy declared reaches, while the sender only uses the current offered set.

## Impact

- **Code**: `Config/ModConstants.cs` (offered set and default), `Config/ModConfig.cs` (normalization and next-radius cycle), `Network/PacketValidator.cs` (legacy acceptance), GMCM/README text, and tests.
- **Network**: the packet wire format does not change; only the accepted value set and compatibility behavior change. Mixed-version sessions cannot use the new `30` reach until the old peer updates.
- **Config migration**: old `6/10/20` values normalize to `3/15/30` on load; the normalization is logged and does not prevent startup.
- **UI**: no layout changes; the HUD and mute menu display the new values and use `30` as the unknown-reach fallback.
- **Tests**: radius set/cycle/normalization, packet validator legacy acceptance, and the existing proximity math with the new values.
