using System;
using System.Collections.Generic;

namespace ProximityVoiceChat.Audio;

/// <summary>Holds locally captured frames for the microphone-test monitor and its level meter.</summary>
public sealed class LocalMonitorSource : IAudioFrameSource
{
    private readonly object _gate = new();
    private readonly Queue<float[]> _frames = new();
    private readonly float[] _frame = new float[ModConstants.FrameSamples];
    private int _frameFill;
    private volatile float _level;
    private volatile bool _monitoringEnabled;

    public float Volume => 1f;

    /// <summary>Latest RMS level of a complete frame.</summary>
    public float Level => this._level;

    /// <summary>Whether captured frames are queued for local playback.</summary>
    public bool MonitoringEnabled
    {
        get => this._monitoringEnabled;
        set
        {
            this._monitoringEnabled = value;
            if (!value)
            {
                lock (this._gate)
                    this._frames.Clear();
            }
        }
    }

    /// <summary>Consume a capture callback chunk, update the meter and queue it when monitoring.</summary>
    public void AppendSamples(float[] samples, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int copy = Math.Min(count - offset, this._frame.Length - this._frameFill);
            Array.Copy(samples, offset, this._frame, this._frameFill, copy);
            this._frameFill += copy;
            offset += copy;

            if (this._frameFill == this._frame.Length)
            {
                this._level = ActivationDecision.CalculateRms(this._frame);

                if (this._monitoringEnabled)
                {
                    float[] frame = new float[this._frame.Length];
                    Array.Copy(this._frame, frame, frame.Length);

                    lock (this._gate)
                    {
                        while (this._frames.Count >= 5)
                            this._frames.Dequeue();

                        this._frames.Enqueue(frame);
                    }
                }

                this._frameFill = 0;
            }
        }
    }

    public bool TryReadFrame(Span<float> destination, double nowSeconds)
    {
        lock (this._gate)
        {
            if (this._frames.Count == 0)
                return false;

            float[] frame = this._frames.Dequeue();
            frame.AsSpan().CopyTo(destination);
            return true;
        }
    }

    public void Clear()
    {
        lock (this._gate)
            this._frames.Clear();

        this._frameFill = 0;
        this._level = 0f;
    }
}
