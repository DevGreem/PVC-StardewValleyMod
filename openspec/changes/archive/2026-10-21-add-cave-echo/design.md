# Design

## Context

See `proposal.md` for motivation. Today `EnvironmentAcoustics` applies a fixed gain (`0.55`) and a low-pass ceiling (`0.35`) per speaker when the listener is in a cave. `PlaybackMixer` only sums the per-speaker frames, normalizes if needed and applies the master volume; it has no effect chain. The same mixer is also used for the local microphone test monitor, which must stay clean.

The cave detection is already in `EnvironmentAcoustics.IsCave` (`MineShaft`, `FarmCave`, `IslandFarmCave`). This change keeps that detection and moves the cave character from “much quieter” to “short echo with mild attenuation”.

## Goals / Non-Goals

**Goals:**

- Add an audible but intelligible short echo/reverb in caves.
- Keep the dry voice present; the cave should not sound like a wall muffling.
- Preserve distance, obstacle and master-volume behavior.
- Keep the effect local and out of the microphone test monitor.
- Keep the audio thread allocation-free.

**Non-Goals:**

- Full convolution reverb, HRTF, stereo reverb or room geometry.
- Echo in outdoor locations or buildings that are not caves.
- Per-speaker echo tails; one mixed room effect is enough for this iteration.
- A new setting, network message or protocol change.
- Changing cave detection.

## Decisions

### D1 — Apply one echo to the mixed output in `PlaybackMixer`

After summing the per-speaker frames (which already include distance and obstacle gains), run the mix through a mono feedback-delay echo. The existing peak limiter and master volume remain after the echo.

- **Why:** it creates a single room effect, costs one delay line and preserves the per-speaker gain decisions. The echo cannot make a distant or occluded speaker louder because it is applied after those gains.
- **Alternative rejected:** a per-speaker echo sounds more diffuse but needs a delay buffer and filter state per channel and multiplies the effect on every speaker.
- **Alternative rejected:** reverb on the capture side would transmit the effect to other players and change the voice data; the effect must be local.

### D2 — Use a short, bounded feedback echo

Add a `CaveEcho` processor with a preallocated mono delay buffer:

- delay ≈ 220 ms (about 10,560 samples at 48 kHz);
- feedback ≈ 0.28;
- wet mix ≈ 0.22;
- a one-pole low-pass in the feedback path (alpha ≈ 0.60) to soften repeats;
- dry voice remains at full level.

`Process` writes `input + filteredDelay * feedback` into the delay buffer and outputs `input + filteredDelay * wet`. The existing limiter handles the extra tail energy.

- **Why:** a single short tap plus feedback reads as an enclosed space without the cost and complexity of a reverb network.
- **Alternative rejected:** a longer or louder echo makes speech muddy and increases the chance of clipping.
- **Consequence:** the effect is subtle; the cave gain/low-pass values are tuned so the echo is the dominant cue.

### D3 — Change the cave preset from “much quieter” to “echo plus mild attenuation”

Keep cave detection, but change `EnvironmentAcoustics`:

- cave gain ≈ `0.80` instead of `0.55`;
- cave low-pass alpha ≈ `0.55` instead of `0.35`;
- the echo supplies the cave character.

- **Why:** the user asked for echo rather than just lower volume; the dry voice must remain intelligible.
- **Alternative rejected:** removing attenuation entirely makes the echo louder than the dry voice and can overwhelm speech.
- **Alternative rejected:** keeping the old `0.55` gain plus echo makes caves too quiet and muddy.

### D4 — Expose cave mode explicitly on the mixer

Add `PlaybackMixer.SetCaveMode(bool)`. `ModEntry.UpdateSpeakerState` sets it from `EnvironmentAcoustics.IsCave(Game1.player.currentLocation)`. The microphone test setup (`StartMicrophoneTest`, `ConfigureTestMonitor`, `StopMicrophoneTest`) forces cave mode off while the local monitor is the source. When the test ends, the next speaker-state tick restores cave mode from the location.

- **Why:** the mixer cannot infer whether its sources are remote speakers or the local test monitor; an explicit flag is unambiguous.
- **Alternative rejected:** enabling echo whenever the listener is in a cave regardless of source would process the microphone test monitor.
- **Consequence:** the echo tail is reset when cave mode is disabled, so leaving a cave or starting a test does not leave stale repeats.

### D5 — No new setting or network state

The cave effect is automatic, local and independent of the host's `WallOcclusion` setting. There is no new config field, packet field or SMAPI message.

- **Why:** it is an environment property, not a player preference, and the host already controls only wall occlusion.
- **Alternative rejected:** a host-controlled cave toggle adds protocol and UI work for no requested behavior.

### D6 — Tests

- `CaveEcho` tests: an impulse produces a delayed tail at the expected offset; feedback decays; disabled mode is passthrough; reset clears the buffer; output stays finite.
- `PlaybackMixer` tests: cave mode adds a tail after the dry mix; master volume still scales the echoed output; disabling cave mode removes the effect.
- `EnvironmentAcoustics` tests: updated cave gain/low-pass values and cave composition.
- Manual check: in a mine/farm cave, remote voice has a short echo and remains understandable; in the open there is no echo; the microphone test monitor has no echo.

## Risks / Trade-offs

- **Echo makes speech muddy** → Use a short delay, low wet mix and low feedback; tune after listening tests.
- **Feedback grows or clips** → Keep feedback well below 1.0, preallocate the buffer, and leave the existing limiter after the echo.
- **Stale tail after leaving a cave** → Reset the echo when cave mode is disabled or the source becomes the test monitor.
- **Echo in the microphone test** → Explicitly force cave mode off while the test monitor is active.
- **Several speakers in a cave** → One global echo is applied to the mixed room; the concurrent-speaker cap already bounds density.
- **Audio-thread cost/allocation** → One preallocated mono delay buffer and one one-pole state; no allocation in `Mix`.
- **Cave gain tuning** → The constants are isolated in `EnvironmentAcoustics`; the effect can be tuned without changing the spec.

## Migration Plan

- No config or network migration. The cave behavior changes for everyone automatically.
- Rollback: remove the echo processor, restore the previous cave gain and low-pass, and leave `SetCaveMode` unused.
