using System;

namespace ProximityVoiceChat.Audio;

/// <summary>A short mono feedback echo used as the cave environment effect.</summary>
public sealed class CaveEcho
{
    public const float DelaySeconds = 0.22f;
    public const float Feedback = 0.28f;
    public const float Wet = 0.22f;
    public const float FeedbackLowPassAlpha = 0.60f;

    private readonly float[] _buffer;
    private int _index;
    private float _feedbackState;

    public CaveEcho(int sampleRate = ModConstants.SampleRate)
    {
        int delaySamples = Math.Max(1, (int)(sampleRate * DelaySeconds));
        this._buffer = new float[delaySamples];
    }

    /// <summary>Number of samples in the delay line.</summary>
    public int DelaySamples => this._buffer.Length;

    /// <summary>Add the echo tail to a frame in place.</summary>
    public void Process(Span<float> samples)
    {
        float[] buffer = this._buffer;
        int index = this._index;
        float feedbackState = this._feedbackState;

        for (int i = 0; i < samples.Length; i++)
        {
            float input = samples[i];
            float delayed = buffer[index];
            feedbackState = (FeedbackLowPassAlpha * delayed) + ((1f - FeedbackLowPassAlpha) * feedbackState);
            buffer[index] = input + (feedbackState * Feedback);
            samples[i] = input + (delayed * Wet);

            index++;
            if (index >= buffer.Length)
                index = 0;
        }

        this._index = index;
        this._feedbackState = feedbackState;
    }

    /// <summary>Clear the delay line and feedback state.</summary>
    public void Reset()
    {
        Array.Clear(this._buffer, 0, this._buffer.Length);
        this._index = 0;
        this._feedbackState = 0f;
    }
}
