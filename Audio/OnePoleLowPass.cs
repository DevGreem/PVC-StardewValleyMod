using System;

namespace ProximityVoiceChat.Audio;

/// <summary>A one-pole low-pass filter with a short alpha slew to avoid clicks.</summary>
public sealed class OnePoleLowPass
{
    private const float AlphaSlew = 0.1f;
    private const float PassthroughThreshold = 0.99f;

    private float _alpha = 1f;
    private float _state;

    /// <summary>Current smoothed alpha; 1 means passthrough.</summary>
    public float Alpha => this._alpha;

    /// <summary>Filter one frame toward the target alpha.</summary>
    public void Process(Span<float> samples, float targetAlpha)
    {
        targetAlpha = Math.Clamp(targetAlpha, 0f, 1f);
        this._alpha += (targetAlpha - this._alpha) * AlphaSlew;

        if (this._alpha >= PassthroughThreshold)
            return;

        float alpha = this._alpha;
        float oneMinusAlpha = 1f - alpha;
        float state = this._state;
        for (int i = 0; i < samples.Length; i++)
        {
            state = (alpha * samples[i]) + (oneMinusAlpha * state);
            samples[i] = state;
        }

        this._state = state;
    }

    /// <summary>Reset the filter to passthrough and clear its state.</summary>
    public void Reset()
    {
        this._alpha = 1f;
        this._state = 0f;
    }
}
