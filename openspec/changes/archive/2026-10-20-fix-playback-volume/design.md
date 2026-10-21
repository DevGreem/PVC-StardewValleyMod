# Design

## Context

See `proposal.md`. The current path is:

1. `ModEntry.UpdateSpeakerState` computes a channel gain that already includes `PlaybackVolume / 100`.
2. `PlaybackMixer.Mix` sums the channels and, if the peak exceeds `1.0`, scales the whole frame by `1 / peak`.

On a hot mix the normalization factor absorbs any reduction of the master volume: lowering the slider from `100` to `60` can still produce a normalized peak of `1.0`. The same normalization is what keeps overlapping voices from clipping, so it cannot simply be removed.

## Goals / Non-Goals

**Goals:**

- Make the master volume scale the final mixed output linearly, including when the limiter is active.
- Keep zero volume silent and keep the mix within `[-1, 1]`.
- Add regression tests that would fail with the current order.

**Non-Goals:**

- No change to distance falloff, speaker selection, jitter buffering, device selection or the config UI.
- No change to the wire format, Opus settings or capture path.
- Not redesigning the limiter beyond what is needed to preserve the master-volume contract.

## Decisions

### D1 — Per-speaker gain contains only distance falloff

`SpeakerChannel.Volume` becomes the distance-based gain (same-location gate and radius falloff). The master volume leaves `ProximityMath.CalculateVolume` and is supplied to the mixer separately.

- **Why:** the master volume must be independent of how hot the pre-master mix is.
- **Alternative rejected:** keep the master in the channel gain and change the limiter to a soft curve. It could still compress loud mixes unpredictably and makes the volume contract harder to test.

### D2 — Apply the master volume after the anti-clipping stage

`PlaybackMixer.Mix` sums the distance-weighted voices, applies the existing peak normalization when needed, then multiplies every sample by the master volume (clamped to `0..1`).

- **Why:** normalization then becomes independent of the slider, and the final multiplication always changes the output. Since the master is `<= 1`, it cannot push a normalized frame above the output range.
- **Alternative rejected:** remove normalization and hard-clamp. That would clip and violate the existing requirement.
- **Alternative rejected:** fixed headroom division. It would lower all audio permanently and still allow clipping with many loud speakers.

### D3 — Publish the master volume to the audio thread

`PlaybackMixer` gets a `SetMasterVolume(float)` method with a volatile backing field. `ModEntry.UpdateSpeakerState` (game thread) sets it from `PlaybackVolume` every tick or when the config changes; `Mix` (audio thread) reads it without allocation or locking.

- **Why:** the mixer runs inside the audio callback and must not fetch game config there.
- **Consequence:** a volume change is audible on the next mixed frame, without restarting streams.

### D4 — Regression tests

Add tests that:
- create a hot mix (one loud speaker or several overlapping speakers);
- assert the output peak is lower with master `0.5` than with `1.0`, approximately half;
- assert master `0.0` produces silence;
- assert the normalized output never exceeds `1.0` before the master is applied.

- **Why:** the existing tests only check `CalculateVolume` arithmetic and do not exercise the mixer's normalization path.

## Risks / Trade-offs

- **[The limiter still normalizes relative loudness when many voices overlap]** → Accepted; it is required to avoid clipping. The master volume now remains independent and predictable.
- **[A volume change is applied on the next game tick/mixed frame]** → Acceptable and already how the current live settings behave.
- **[A future limiter redesign could reintroduce the bug]** → The new tests assert the master scales a hot mix, so a regression fails CI.
- **[Applying master after normalization changes loudness at high volumes]** → It makes the control work as documented; no settings migration is needed.

## Migration Plan

- No config change and no migration.
- Deploy the updated DLL; the existing `PlaybackVolume` value keeps its meaning.
- Rollback is the previous DLL; no data changes.

## Open Questions

None.
