using System;
using System.Diagnostics;
using System.Threading;
using ProximityVoiceChat.Config;
using ProximityVoiceChat.Network;

namespace ProximityVoiceChat.Audio;

/// <summary>
/// Turns microphone callback samples into fixed 20 ms frames, applies the activation
/// rule, encodes active frames and hands packets to the transport.
/// </summary>
public sealed class CapturePipeline : IDisposable
{
    private readonly VoiceEncoder _encoder = new();
    private readonly Action<VoicePacket> _onPacket;
    private readonly float[] _frame = new float[ModConstants.FrameSamples];
    private readonly byte[] _encoded = new byte[VoicePacket.MaxAudioBytes];

    private int _frameFill;
    private ulong _sequence;
    private long _framesFilled;
    private long _framesTransmitted;
    private long _encodeFailures;
    private long _lastTransmitTimestamp;
    private volatile bool _microphoneEnabled;
    private volatile bool _sessionActive;
    private volatile bool _pushToTalkHeld;
    private volatile int _activationMode = (int)Config.ActivationMode.VoiceActivity;
    private volatile int _sensitivity = ModConstants.DefaultSensitivity;
    private volatile int _radius = ModConstants.DefaultProximityRadius;
    private volatile string _locationName = "";

    public CapturePipeline(Action<VoicePacket> onPacket)
    {
        this._onPacket = onPacket ?? throw new ArgumentNullException(nameof(onPacket));
    }

    public bool MicrophoneEnabled
    {
        get => this._microphoneEnabled;
        set => this._microphoneEnabled = value;
    }

    /// <summary>True only while a multiplayer world is loaded.</summary>
    public bool SessionActive
    {
        get => this._sessionActive;
        set => this._sessionActive = value;
    }

    public bool PushToTalkHeld
    {
        get => this._pushToTalkHeld;
        set => this._pushToTalkHeld = value;
    }

    public Config.ActivationMode ActivationMode
    {
        get => (Config.ActivationMode)this._activationMode;
        set => this._activationMode = (int)value;
    }

    public int Sensitivity
    {
        get => this._sensitivity;
        set => this._sensitivity = value;
    }

    public int ProximityRadius
    {
        get => this._radius;
        set => this._radius = value;
    }

    /// <summary>Name of the local player's current location, captured with every frame.</summary>
    public string LocationName
    {
        get => this._locationName;
        set => this._locationName = value ?? "";
    }

    public long FramesFilled => Interlocked.Read(ref this._framesFilled);

    public long FramesTransmitted => Interlocked.Read(ref this._framesTransmitted);

    public long EncodeFailures => Interlocked.Read(ref this._encodeFailures);

    /// <summary>Get whether a frame was transmitted within the last 300 ms.</summary>
    public bool IsTransmittingNow
    {
        get
        {
            long timestamp = Interlocked.Read(ref this._lastTransmitTimestamp);
            if (timestamp == 0L)
                return false;

            double elapsedSeconds = (Stopwatch.GetTimestamp() - timestamp) / (double)Stopwatch.Frequency;
            return elapsedSeconds <= 0.3d;
        }
    }

    /// <summary>Consume microphone samples from the audio callback.</summary>
    public void AppendSamples(float[] samples, int count)
    {
        if (!this._microphoneEnabled || !this._sessionActive)
        {
            this._frameFill = 0;
            return;
        }

        int offset = 0;
        while (offset < count)
        {
            int copy = Math.Min(count - offset, this._frame.Length - this._frameFill);
            Array.Copy(samples, offset, this._frame, this._frameFill, copy);
            this._frameFill += copy;
            offset += copy;

            if (this._frameFill == this._frame.Length)
            {
                Interlocked.Increment(ref this._framesFilled);
                this.ProcessFrame(this._frame);
                this._frameFill = 0;
            }
        }
    }

    /// <summary>Start a new world session: drop any partial frame and restart sequence numbers.</summary>
    public void ResetSession()
    {
        this._frameFill = 0;
        this._sequence = 0;
        this._encoder.Reset();
        Interlocked.Exchange(ref this._lastTransmitTimestamp, 0L);
    }

    public void Dispose()
    {
        this._encoder.Dispose();
    }

    private void ProcessFrame(float[] frame)
    {
        float rms = ActivationDecision.CalculateRms(frame);
        bool transmit = ActivationDecision.ShouldTransmit(
            this._microphoneEnabled,
            this._sessionActive,
            this.ActivationMode,
            this._pushToTalkHeld,
            rms,
            this._sensitivity);

        if (!transmit)
            return;

        int length = this._encoder.Encode(frame, this._encoded);
        if (length <= 0 || length > this._encoded.Length)
        {
            Interlocked.Increment(ref this._encodeFailures);
            return;
        }

        string location = this._locationName;
        if (!VoicePacket.IsValidLocationName(location))
            return;

        byte[] audio = new byte[length];
        Array.Copy(this._encoded, audio, length);

        VoicePacket packet = new(
            VoicePacket.CurrentVersion,
            this._sequence++,
            this._radius,
            location,
            audio);

        Interlocked.Increment(ref this._framesTransmitted);
        Interlocked.Exchange(ref this._lastTransmitTimestamp, Stopwatch.GetTimestamp());
        this._onPacket(packet);
    }
}
