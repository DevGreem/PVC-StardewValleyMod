# Design

## Context

See `proposal.md`. The current implementation uses `ModConfig.ProximityRadius` in two places:

- `ModEntry.UpdateSpeakerState` computes the local player's listening gate and volume falloff from it.
- `CapturePipeline` stamps the same value into outgoing packets.

The receiver validates the packet radius but does not use it for playback. As a result the setting behaves as a listener preference, not as the reach of the local voice. The offered set is `3/6/10/15/20`.

## Goals / Non-Goals

**Goals:**

- Make the setting mean “how far my voice carries”.
- Use the sender's declared, validated radius on the receiving side.
- Reduce the choice to three values and migrate old values safely.
- Keep the indicator truthful: show outgoing reach and list speakers using their own declared radius.

**Non-Goals:**

- No wire-format change; the packet already carries an integer radius.
- No per-listener incoming filter or per-player mute in this change.
- No new radius values beyond `3/10/20`.
- No change to same-location gating, codec, transport rate limits or microphone activation.

## Decisions

### D1 — The packet's declared radius is the playback reach

`OnVoicePacketReceived` stores the packet radius on the speaker's channel. `UpdateSpeakerState` uses that value for the same-location/distance gate and volume falloff. The local `ProximityRadius` is no longer read when deciding what to play.

- **Why:** this is the only way changing your radius changes how far others hear you.
- **Alternative rejected:** keep the local radius as the listening gate and also send a radius. That is the current confusing behavior.
- **Alternative rejected:** use `min(senderRadius, listenerRadius)`. It respects a listener preference, but it means a listener can silently override the reach the speaker chose; the requested model is speaker-controlled. A future per-listener mute/filter can be added without changing this contract.

### D2 — Offer exactly `3`, `10` and `20` tiles, default `10`

`ModConstants.ProximityRadii` becomes `{3, 10, 20}` and `DefaultProximityRadius` stays `10`. `ModConfig.Normalize` already resets values outside the set, so old `6`/`15` values become `10` on load; `20` remains valid.

- **Why these three:** `3` covers close/private conversations, `10` is a normal conversational range and `20` is long range. Fewer choices make the trade-off easy to understand.
- **Why keep `10` as default:** it preserves the current documented default and represents the “normal” case; players choose `3` for privacy or `20` for reach.
- **Alternative rejected:** a free numeric slider. The receiver needs a closed set to validate, and the current UI/cycle key already assumes a fixed set.

### D3 — Store the declared radius per speaker channel

`SpeakerChannel` gains a `ProximityRadius` value set from the accepted packet. The snapshot used by the mixer and the indicator is built from that value. A speaker can change radius mid-session; the newest packet updates the channel.

- **Why:** two speakers may use different radii at the same time; the local config cannot represent that.
- **Consequence:** the distance falloff and “nearby” indicator become per-speaker rather than global.

### D4 — The HUD shows outgoing reach and per-speaker range

The “Radius” line displays the local `ProximityRadius` as outgoing reach. The nearby-speaker list includes a speaker only when the local player is inside that speaker's declared radius and the same location; the local radius no longer filters the list.

- **Why:** the indicator must not claim that a speaker is out of range when the speaker's own packet says otherwise.
- **Alternative rejected:** show both a local and a remote radius per speaker. Useful, but noisy and not required for the first version.

### D5 — Tests

- Packet radius controls `SpeakerChannel`/`UpdateSpeakerState`; a smaller local radius does not silence an in-range speaker.
- `ModConfig` accepts `3/10/20`, rejects `6/15` (and any other value), and normalizes old values to `10`.
- `NextRadius` cycles only the three values.
- Indicator rules use the speaker's declared radius, not the local one.

## Risks / Trade-offs

- **[A listener can hear a speaker farther than the listener's own radius]** → This is the deliberate meaning of the setting; the README and tooltip must explain that it is outgoing reach, not an incoming filter.
- **[A player expected their radius to limit what they hear]** → Add a clear label/tooltip (`Voice reach`) and a README migration note. A separate per-listener filter can be a future capability.
- **[Old configs with `6`/`15` lose their value]** → Normalize to `10` and log the normalization; document it. `20` stays valid.
- **[A modified client claims the maximum `20`]** → The closed set caps reach at `20`; an arbitrary value is rejected by the existing transport validation.
- **[Speaker changes radius mid-sentence]** → The channel uses the newest accepted packet; already-buffered frames keep the previous gain until replaced. Acceptable and bounded by the jitter buffer.

## Migration Plan

- Existing configs keep `3`, `10` or `20` unchanged; `6`/`15` normalize to `10`.
- No wire-format bump: the radius field is unchanged and still validated against the new set.
- Rollback restores the previous set and listener-controlled behavior; configs with `3/10/20` remain valid.

## Open Questions

None.
