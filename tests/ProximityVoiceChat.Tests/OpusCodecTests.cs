namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat;
using ProximityVoiceChat.Audio;
using ProximityVoiceChat.Network;
using Xunit;

public class OpusCodecTests
{
    [Fact]
    public void RoundTrip_DecodesFramesAndKeepsSignal()
    {
        using VoiceEncoder encoder = new();
        using VoiceDecoder decoder = new();
        float[] input = CreateSine(0.25f);
        byte[] encoded = new byte[VoicePacket.MaxAudioBytes];
        float[] decoded = new float[ModConstants.FrameSamples];

        int length = encoder.Encode(input, encoded);
        Assert.True(length > 0);
        Assert.True(length <= VoicePacket.MaxAudioBytes);

        int decodedSamples = decoder.Decode(encoded.AsSpan(0, length), decoded);
        Assert.Equal(ModConstants.FrameSamples, decodedSamples);
        Assert.True(ActivationDecision.CalculateRms(decoded) > 0.001f);
    }

    [Fact]
    public void EncodedPackets_StayWithinWireCap()
    {
        using VoiceEncoder encoder = new();
        byte[] output = new byte[VoicePacket.MaxAudioBytes];
        float[][] signals =
        {
            new float[ModConstants.FrameSamples],
            CreateSpeechLikeNoise(),
            CreateSine(0.95f)
        };

        foreach (float[] signal in signals)
        {
            for (int frame = 0; frame < 50; frame++)
            {
                int length = encoder.Encode(signal, output);
                Assert.True(length > 0);
                Assert.True(length <= VoicePacket.MaxAudioBytes);
            }
        }
    }

    [Fact]
    public void Decode_OfEmptyPayloadProducesConcealedFrame()
    {
        using VoiceDecoder decoder = new();
        float[] output = new float[ModConstants.FrameSamples];

        int decoded = decoder.Decode(ReadOnlySpan<byte>.Empty, output);

        Assert.True(decoded > 0);
        Assert.Equal(ModConstants.FrameSamples, decoded);
    }

    [Fact]
    public void Encoder_RejectsWrongFrameLength()
    {
        using VoiceEncoder encoder = new();
        byte[] output = new byte[VoicePacket.MaxAudioBytes];

        Assert.Equal(0, encoder.Encode(new float[ModConstants.FrameSamples - 1], output));
    }

    [Fact]
    public void Decoder_RejectsGarbageWithoutThrowing()
    {
        using VoiceDecoder decoder = new();
        float[] output = new float[ModConstants.FrameSamples];

        int decoded = decoder.Decode(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, output);

        Assert.Equal(0, decoded);
    }

    private static float[] CreateSine(float amplitude)
    {
        float[] samples = new float[ModConstants.FrameSamples];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = amplitude * MathF.Sin(2f * MathF.PI * 440f * i / ModConstants.SampleRate);

        return samples;
    }

    private static float[] CreateSpeechLikeNoise()
    {
        Random random = new(1234);
        float[] samples = new float[ModConstants.FrameSamples];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = (float)((random.NextDouble() * 2d - 1d) * 0.3d);

        return samples;
    }
}
