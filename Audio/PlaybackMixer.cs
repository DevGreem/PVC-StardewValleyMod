using System;

namespace ProximityVoiceChat.Audio;

/// <summary>Mixes the closest active speakers into one mono frame. Runs only on the audio output thread.</summary>
public sealed class PlaybackMixer
{
    private readonly float[] _frame = new float[ModConstants.FrameSamples];
    private readonly CaveEcho _caveEcho = new();
    private volatile IAudioFrameSource[] _sources = Array.Empty<IAudioFrameSource>();
    private volatile float _masterVolume = 1f;
    private volatile bool _caveMode;
    private bool _lastCaveMode;

    /// <summary>Replace the bounded, distance-ordered snapshot of speakers to mix.</summary>
    public void SetChannels(SpeakerChannel[] channels)
    {
        this._sources = channels;
    }

    /// <summary>Replace the mixer sources, e.g. with a local microphone-test monitor.</summary>
    public void SetSources(IAudioFrameSource[] sources)
    {
        this._sources = sources;
    }

    /// <summary>Enable or disable the cave echo on the mixed output.</summary>
    public void SetCaveMode(bool enabled)
    {
        this._caveMode = enabled;
    }

    /// <summary>Set the final output gain; applied after the anti-clipping stage.</summary>
    public void SetMasterVolume(float volume)
    {
        this._masterVolume = Math.Clamp(volume, 0f, 1f);
    }

    /// <summary>Fill one frame from the active speakers without allocating or clipping.</summary>
    public void Mix(Span<float> output)
    {
        output.Clear();

        IAudioFrameSource[] sources = this._sources;
        for (int i = 0; i < sources.Length; i++)
        {
            IAudioFrameSource source = sources[i];
            if (source.Volume <= 0f)
                continue;

            if (!source.TryReadFrame(this._frame, GetTimestampSeconds()))
                continue;

            float volume = source.Volume;
            for (int sample = 0; sample < ModConstants.FrameSamples; sample++)
                output[sample] += this._frame[sample] * volume;
        }

        bool caveMode = this._caveMode;
        if (caveMode != this._lastCaveMode)
        {
            this._caveEcho.Reset();
            this._lastCaveMode = caveMode;
        }

        if (caveMode)
            this._caveEcho.Process(output);

        // Normalize only when the mix would exceed the output range, so quiet voices
        // keep their level instead of being attenuated by a fixed headroom factor.
        float peak = 0f;
        for (int i = 0; i < output.Length; i++)
        {
            float magnitude = MathF.Abs(output[i]);
            if (magnitude > peak)
                peak = magnitude;
        }

        if (peak > 1f)
        {
            float scale = 1f / peak;
            for (int i = 0; i < output.Length; i++)
                output[i] *= scale;
        }

        // The master volume is applied after limiting. This keeps the slider
        // independent from how hot the pre-master mix was.
        float masterVolume = this._masterVolume;
        for (int i = 0; i < output.Length; i++)
            output[i] *= masterVolume;
    }

    private static double GetTimestampSeconds()
    {
        return System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;
    }
}
