namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat;
using ProximityVoiceChat.Audio;
using ProximityVoiceChat.Network;
using Xunit;

public class PlaybackMixerTests
{
    [Fact]
    public void ManyLoudVoices_StayWithinOutputRange()
    {
        List<SpeakerChannel> channels = new();
        try
        {
            for (int i = 0; i < ModConstants.MaxConcurrentSpeakers; i++)
            {
                SpeakerChannel channel = new(i, $"Player {i}") { Volume = 1f };
                foreach (VoicePacket packet in CreatePackets(0, 2))
                    channel.Receive(packet, 0.02d * packet.Sequence);

                channels.Add(channel);
            }

            PlaybackMixer mixer = new();
            mixer.SetMasterVolume(1f);
            mixer.SetChannels(channels.ToArray());
            float[] output = new float[ModConstants.FrameSamples];
            mixer.Mix(output);

            float peak = 0f;
            foreach (float sample in output)
            {
                Assert.True(float.IsFinite(sample));
                peak = MathF.Max(peak, MathF.Abs(sample));
            }

            Assert.True(peak > 0f);
            Assert.True(peak <= 1f, $"mix peaked at {peak}");
        }
        finally
        {
            foreach (SpeakerChannel channel in channels)
                channel.Dispose();
        }
    }

    [Fact]
    public void MasterVolume_ScalesHotMixAfterLimiting()
    {
        List<SpeakerChannel> channels = CreateChannels(4);
        try
        {
            PlaybackMixer mixer = new();
            mixer.SetChannels(channels.ToArray());
            float[] output = new float[ModConstants.FrameSamples];

            mixer.SetMasterVolume(1f);
            mixer.Mix(output);
            float fullPeak = Peak(output);
            Assert.InRange(fullPeak, 0.99f, 1f);

            mixer.SetMasterVolume(0.5f);
            mixer.Mix(output);
            float halfPeak = Peak(output);
            Assert.InRange(halfPeak, fullPeak * 0.45f, fullPeak * 0.55f);

            mixer.SetMasterVolume(0f);
            mixer.Mix(output);
            Assert.All(output, sample => Assert.Equal(0f, sample));
        }
        finally
        {
            foreach (SpeakerChannel channel in channels)
                channel.Dispose();
        }
    }

    [Fact]
    public void MasterVolume_ClampsOutOfRangeValues()
    {
        using SpeakerChannel channel = new(1, "Alice") { Volume = 1f };
        foreach (VoicePacket packet in CreatePackets(0, 2))
            channel.Receive(packet, 0.02d * packet.Sequence);

        PlaybackMixer mixer = new();
        mixer.SetChannels(new[] { channel });
        float[] output = new float[ModConstants.FrameSamples];

        mixer.SetMasterVolume(2f);
        mixer.Mix(output);
        Assert.True(Peak(output) <= 1f);

        mixer.SetMasterVolume(-1f);
        mixer.Mix(output);
        Assert.All(output, sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public void MutedSpeaker_ProducesSilence()
    {
        using SpeakerChannel channel = new(1, "Alice") { Volume = 0f };
        foreach (VoicePacket packet in CreatePackets(0, 2))
            channel.Receive(packet, 0.02d * packet.Sequence);

        PlaybackMixer mixer = new();
        mixer.SetChannels(new[] { channel });
        float[] output = new float[ModConstants.FrameSamples];
        mixer.Mix(output);

        Assert.All(output, sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public void CaveMode_AddsAnEchoTailAfterTheDryMix()
    {
        using SpeakerChannel channel = new(1, "Alice") { Volume = 1f };
        foreach (VoicePacket packet in CreatePackets(0, 2))
            channel.Receive(packet, 0.02d * packet.Sequence);

        PlaybackMixer mixer = new();
        mixer.SetCaveMode(true);
        mixer.SetChannels(new[] { channel });
        float[] output = new float[ModConstants.FrameSamples];
        mixer.Mix(output);
        Assert.True(Peak(output) > 0f);

        mixer.SetChannels(Array.Empty<SpeakerChannel>());
        bool heardTail = false;
        for (int frame = 0; frame < 20; frame++)
        {
            mixer.Mix(output);
            if (Peak(output) > 0f)
            {
                heardTail = true;
                break;
            }
        }

        Assert.True(heardTail);
    }

    [Fact]
    public void CaveModeDisabled_HasNoTail()
    {
        using SpeakerChannel channel = new(1, "Alice") { Volume = 1f };
        foreach (VoicePacket packet in CreatePackets(0, 2))
            channel.Receive(packet, 0.02d * packet.Sequence);

        PlaybackMixer mixer = new();
        mixer.SetChannels(new[] { channel });
        float[] output = new float[ModConstants.FrameSamples];
        mixer.Mix(output);

        mixer.SetChannels(Array.Empty<SpeakerChannel>());
        for (int frame = 0; frame < 20; frame++)
        {
            mixer.Mix(output);
            Assert.All(output, sample => Assert.Equal(0f, sample));
        }
    }

    [Fact]
    public void DisablingCaveMode_ResetsTheTail()
    {
        using SpeakerChannel channel = new(1, "Alice") { Volume = 1f };
        foreach (VoicePacket packet in CreatePackets(0, 2))
            channel.Receive(packet, 0.02d * packet.Sequence);

        PlaybackMixer mixer = new();
        mixer.SetCaveMode(true);
        mixer.SetChannels(new[] { channel });
        float[] output = new float[ModConstants.FrameSamples];
        mixer.Mix(output);

        mixer.SetChannels(Array.Empty<SpeakerChannel>());
        mixer.SetCaveMode(false);
        mixer.Mix(output);

        Assert.All(output, sample => Assert.Equal(0f, sample));
    }

    private static List<SpeakerChannel> CreateChannels(int count)
    {
        VoicePacket[] packets = CreatePackets(0, 2);
        List<SpeakerChannel> channels = new(count);

        for (int i = 0; i < count; i++)
        {
            SpeakerChannel channel = new(i, $"Player {i}") { Volume = 1f };
            foreach (VoicePacket packet in packets)
                channel.Receive(packet, 0.02d * packet.Sequence);

            channels.Add(channel);
        }

        return channels;
    }

    private static float Peak(float[] output)
    {
        float peak = 0f;
        foreach (float sample in output)
            peak = MathF.Max(peak, MathF.Abs(sample));

        return peak;
    }

    private static VoicePacket[] CreatePackets(ulong start, ulong end)
    {
        VoicePacket[] packets = new VoicePacket[end - start + 1];
        using VoiceEncoder encoder = new();
        byte[] encoded = new byte[VoicePacket.MaxAudioBytes];
        float[] signal = new float[ModConstants.FrameSamples];
        for (int i = 0; i < signal.Length; i++)
            signal[i] = 0.9f * MathF.Sin(2f * MathF.PI * 220f * i / ModConstants.SampleRate);

        int length = encoder.Encode(signal, encoded);
        for (ulong sequence = start; sequence <= end; sequence++)
        {
            byte[] audio = new byte[length];
            Array.Copy(encoded, audio, length);
            packets[sequence - start] = new VoicePacket(VoicePacket.CurrentVersion, sequence, 10, "Town", audio);
        }

        return packets;
    }
}
