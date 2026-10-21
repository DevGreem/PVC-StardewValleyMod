using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Concentus;
using Concentus.Enums;
using Concentus.Structs;

namespace ProximityVoiceChat.Audio;

/// <summary>Thin wrapper around Concentus' Opus encoder for one voice stream.</summary>
public sealed class VoiceEncoder : IDisposable
{
    private readonly IOpusEncoder _encoder;

    public VoiceEncoder()
    {
        OpusCodecFactory.AttemptToUseNativeLibrary = false;
        this._encoder = OpusCodecFactory.CreateEncoder(ModConstants.SampleRate, ModConstants.Channels, OpusApplication.OPUS_APPLICATION_VOIP);
        this._encoder.Bitrate = ModConstants.Bitrate;
        this._encoder.Complexity = 5;
        this._encoder.UseInbandFEC = true;
        this._encoder.PacketLossPercent = 10;
        this._encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
    }

    /// <summary>Encode one 20 ms frame; returns the number of bytes written, or 0 on failure.</summary>
    public int Encode(ReadOnlySpan<float> pcm, Span<byte> output)
    {
        if (pcm.Length != ModConstants.FrameSamples || output.Length == 0)
            return 0;

        try
        {
            return this._encoder.Encode(pcm, ModConstants.FrameSamples, output, output.Length);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>Reset encoder prediction so a new session does not inherit old audio.</summary>
    public void Reset()
    {
        this._encoder.ResetState();
    }

    public void Dispose()
    {
        this._encoder.Dispose();
    }
}

/// <summary>Thin wrapper around Concentus' Opus decoder for one speaker.</summary>
public sealed class VoiceDecoder : IDisposable
{
    private readonly IOpusDecoder _decoder;

    public VoiceDecoder()
    {
        OpusCodecFactory.AttemptToUseNativeLibrary = false;
        this._decoder = OpusCodecFactory.CreateDecoder(ModConstants.SampleRate, ModConstants.Channels);
    }

    /// <summary>Decode an encoded frame, or conceal one lost frame when the payload is empty.</summary>
    public int Decode(ReadOnlySpan<byte> packet, Span<float> pcm)
    {
        try
        {
            return this._decoder.Decode(packet, pcm, ModConstants.FrameSamples, decode_fec: false);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>Reset decoder state so a new sender stream does not inherit old prediction.</summary>
    public void Reset()
    {
        this._decoder.ResetState();
    }

    public void Dispose()
    {
        this._decoder.Dispose();
    }
}
