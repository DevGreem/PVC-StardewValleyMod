# Proposal

## Why

Changing `PlaybackVolume` does not always change how loud incoming voice sounds. The mixer applies the master volume while summing speakers and then normalizes the whole mix to the output range; when the mix is hot, that normalization cancels the master-volume change. Players perceive the slider as broken.

## What Changes

- Separate per-speaker distance gain from the master playback volume in the mixer.
- Apply the master volume to the final limited mix, after the anti-clipping stage, so lowering the slider always lowers the output.
- Keep the existing behavior of reaching silence at zero and keeping overlapping voices within range.
- Add regression tests with hot single-speaker and overlapping-speaker mixes at master volumes `1.0`, `0.5` and `0.0`; verify the output scales rather than sticking at the limiter ceiling.
- No config, UI, wire-format, capture or transport changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `proximity-playback`: clarifies that the master playback volume is applied to the final limited mix, so it scales all incoming voice even when the mix is limited.

## Impact

- **Code**: `Audio/PlaybackMixer.cs`, the volume calculation in `ModEntry.UpdateSpeakerState`, `Audio/ProximityMath.cs` and the playback unit tests.
- **Behavior**: the existing volume slider starts affecting loud mixes as expected.
- **Compatibility**: no settings migration; `PlaybackVolume` keeps its name, range and default.
- **Audio**: no clipping regression; the limiter still keeps the mix inside the output range.
