using System;
using System.Collections.Generic;
using ProximityVoiceChat.Config;
using ProximityVoiceChat.Network;

namespace ProximityVoiceChat.Audio;

/// <summary>One speaker's decoder and jitter buffer. Receive runs on the game thread; mixing on the audio thread.</summary>
public sealed class SpeakerChannel : IAudioFrameSource, IDisposable
{
    private readonly object _gate = new();
    private readonly Queue<float[]> _buffer = new();
    private readonly VoiceDecoder _decoder = new();
    private readonly OnePoleLowPass _occlusionFilter = new();
    private ulong? _nextSequence;
    private bool _playbackStarted;
    private volatile float _volume;
    private volatile int _proximityRadius = ModConstants.DefaultProximityRadius;
    private volatile float _occlusionAlphaTarget = 1f;
    private volatile bool _isNearby;
    private double _lastPacketTime;
    private double _lastAudibleTime;
    private double _distanceTiles;

    public SpeakerChannel(long playerId, string playerName)
    {
        this.PlayerId = playerId;
        this.PlayerName = playerName;
    }

    public long PlayerId { get; }

    public string PlayerName { get; set; }

    /// <summary>Playback gain for this speaker, written by the game thread and read by the mixer.</summary>
    public float Volume
    {
        get => this._volume;
        set => this._volume = value;
    }

    public double DistanceTiles
    {
        get => this._distanceTiles;
        set => this._distanceTiles = value;
    }

    /// <summary>Voice reach declared by this speaker in the latest accepted packet, in tiles.</summary>
    public int ProximityRadius
    {
        get => this._proximityRadius;
        set => this._proximityRadius = value;
    }

    /// <summary>Low-pass alpha target for static wall occlusion; 1 means no filtering.</summary>
    public float OcclusionAlphaTarget
    {
        get => this._occlusionAlphaTarget;
        set => this._occlusionAlphaTarget = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>Whether this speaker is in the local location and inside their declared reach.</summary>
    public bool IsNearby
    {
        get => this._isNearby;
        set => this._isNearby = value;
    }

    public double LastPacketTime
    {
        get
        {
            lock (this._gate)
                return this._lastPacketTime;
        }
    }

    public double LastAudibleTime
    {
        get
        {
            lock (this._gate)
                return this._lastAudibleTime;
        }
    }

    /// <summary>Number of decoded frames waiting to be mixed.</summary>
    public int BufferedFrames
    {
        get
        {
            lock (this._gate)
                return this._buffer.Count;
        }
    }

    /// <summary>Get whether this speaker was heard within the speaking hold window.</summary>
    public bool IsSpeaking(double nowSeconds)
    {
        lock (this._gate)
            return this._lastAudibleTime > 0d
                && nowSeconds - this._lastAudibleTime <= ModConstants.SpeakingHoldMilliseconds / 1000d;
    }

    /// <summary>Accept a packet, concealing sequence gaps, and enqueue decoded audio when requested.</summary>
    public bool Receive(VoicePacket packet, double nowSeconds, bool enqueueAudio = true)
    {
        if (!packet.HasSupportedVersion())
            return false;

        lock (this._gate)
        {
            if (this._nextSequence is null)
            {
                this._decoder.Reset();
                this._nextSequence = packet.Sequence;
            }
            else if (packet.Sequence < this._nextSequence.Value)
            {
                return false; // late or duplicate packet
            }
            else if (packet.Sequence > this._nextSequence.Value)
            {
                ulong missing = packet.Sequence - this._nextSequence.Value;
                if (missing > (ulong)ModConstants.MaxBufferedFrames)
                {
                    this._buffer.Clear();
                    this._decoder.Reset();
                }
                else
                {
                    for (ulong i = 0; i < missing; i++)
                        this.DecodeLostFrameLocked(enqueueAudio);
                }

                this._nextSequence = packet.Sequence;
            }

            this.ProximityRadius = packet.ProximityRadius;

            float[] frame = new float[ModConstants.FrameSamples];
            int decoded = this._decoder.Decode(packet.Audio, frame);
            if (decoded > 0 && enqueueAudio)
                this.EnqueueLocked(frame);

            this._nextSequence = packet.Sequence + 1;
            this._lastPacketTime = nowSeconds;
            if (decoded > 0)
                this._lastAudibleTime = nowSeconds;

            return decoded > 0;
        }
    }

    /// <summary>
    /// Pull one decoded frame for mixing. Returns false while the jitter buffer is
    /// prebuffering; a stalled partial buffer is flushed after 100 ms so very short
    /// utterances are not lost.
    /// </summary>
    public bool TryReadFrame(Span<float> destination, double nowSeconds)
    {
        lock (this._gate)
        {
            if (!this._playbackStarted)
            {
                bool enough = this._buffer.Count >= ModConstants.JitterTargetFrames;
                bool stalled = this._buffer.Count > 0
                    && this._lastPacketTime > 0d
                    && nowSeconds - this._lastPacketTime >= 0.1d;

                if (!enough && !stalled)
                    return false;

                this._playbackStarted = true;
            }

            if (this._buffer.Count == 0)
            {
                this._playbackStarted = false;
                return false;
            }

            float[] frame = this._buffer.Dequeue();
            frame.AsSpan().CopyTo(destination);
            this._occlusionFilter.Process(destination.Slice(0, frame.Length), this._occlusionAlphaTarget);
            return true;
        }
    }

    /// <summary>Drop queued playback without resetting the decoder or the speaking state.</summary>
    public void DiscardBufferedAudio()
    {
        lock (this._gate)
        {
            this._buffer.Clear();
            this._playbackStarted = false;
        }
    }

    public void Clear()
    {
        lock (this._gate)
        {
            this._buffer.Clear();
            this._nextSequence = null;
            this._playbackStarted = false;
            this._lastPacketTime = 0d;
            this._lastAudibleTime = 0d;
            this._isNearby = false;
            this._occlusionFilter.Reset();
            this._occlusionAlphaTarget = 1f;
            this._decoder.Reset();
        }
    }

    public void Dispose()
    {
        lock (this._gate)
        {
            this._buffer.Clear();
            this._occlusionFilter.Reset();
            this._decoder.Dispose();
        }
    }

    private void DecodeLostFrameLocked(bool enqueueAudio)
    {
        float[] frame = new float[ModConstants.FrameSamples];
        int decoded = this._decoder.Decode(ReadOnlySpan<byte>.Empty, frame);

        if (decoded <= 0)
            Array.Clear(frame, 0, frame.Length);

        if (enqueueAudio)
            this.EnqueueLocked(frame);
    }

    private void EnqueueLocked(float[] frame)
    {
        while (this._buffer.Count >= ModConstants.MaxBufferedFrames)
            this._buffer.Dequeue();

        this._buffer.Enqueue(frame);
    }
}
