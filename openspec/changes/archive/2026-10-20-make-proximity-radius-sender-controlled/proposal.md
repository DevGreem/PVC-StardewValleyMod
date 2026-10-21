# Proposal

## Why

Today the proximity radius is effectively a **listener** setting: it decides which incoming voices the local player hears, while the radius sent in the packet is only validated. Changing your own radius therefore does not change how far your voice reaches, which is confusing and does not let players choose a private conversation by shrinking their own reach.

## What Changes

- Make `ProximityRadius` the **reach of the local player's voice**. The sender includes it in each voice packet; the receiver uses that declared radius for the same-location gate and distance falloff instead of its own configured radius.
- Reduce the offered radius set to three values: `3`, `10` and `20` tiles. `3` is for close/private conversation, `10` for normal conversation and `20` for long range; `10` stays the default for compatibility.
- Treat the setting as the local player's outgoing reach: changing it changes how far other players can hear the local player, immediately.
- Keep validating the declared radius against the offered set, so a modified client cannot choose an arbitrary reach.
- Update the HUD so the active radius means the local player's voice reach, and nearby speakers are listed only when the speaker's declared radius covers the local player.
- Add tests for packet-radius gating, the reduced set, config normalization of old `6`/`15` values, and indicator selection.
- **BREAKING** (behavioral): the listener's own radius no longer limits incoming voice. A player who wants to hear fewer people must move away, choose a smaller outgoing radius (which does not affect what they hear) or wait for a future per-listener filter. This trade-off is deliberate: the radius now answers “how far does my voice carry?”.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `proximity-playback`: distance falloff and silence now use the radius declared by the speaker, not the listener's configured radius.
- `voice-chat-settings`: the radius setting is the local outgoing reach, its offered set shrinks to `3`/`10`/`20`, and old `6`/`15` values normalize to the default.
- `voice-activity-indicator`: the active radius shown is the local outgoing reach, and nearby speakers are filtered by each speaker's declared radius.

## Impact

- **Code**: `Config/ModConfig.cs` / `ModConstants.ProximityRadii`, packet-to-speaker channel state in `ModEntry`, `Audio/ProximityMath.cs`, `UI/IndicatorRules.cs`, GMCM labels/tooltips and tests.
- **Config migration**: configs containing `6` or `15` are normalized to `10` on the next load; `3`, `10` and `20` remain valid.
- **Privacy**: players opt into a smaller reach by choosing `3`; the receiver no longer has a separate incoming-radius control.
- **Network**: no wire-format change; the packet already carries the sender's radius as a validated integer.
- **Documentation**: README must describe the radius as outgoing reach and document the reduced set and migration.
