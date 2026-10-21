namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat;
using ProximityVoiceChat.Audio;
using ProximityVoiceChat.Network;
using Xunit;

public class SpeakerChannelTests
{
    [Fact]
    public void JitterBuffer_PrebuffersBeforePlayback()
    {
        using SpeakerChannel channel = new(1, "Alice");
        VoicePacket[] packets = CreatePackets(0, 4);

        channel.Receive(packets[0], 0d);
        channel.Receive(packets[1], 0.02d);

        float[] frame = new float[ModConstants.FrameSamples];
        Assert.False(channel.TryReadFrame(frame, 0.03d));
        Assert.False(channel.TryReadFrame(frame, 0.03d));

        channel.Receive(packets[2], 0.04d);
        Assert.True(channel.TryReadFrame(frame, 0.05d));

        channel.Receive(packets[3], 0.06d);
        channel.Receive(packets[4], 0.08d);
        Assert.True(channel.TryReadFrame(frame, 0.07d));
        Assert.True(channel.TryReadFrame(frame, 0.08d));
    }

    [Fact]
    public void LostPacket_IsConcealedAndPlaybackContinues()
    {
        using SpeakerChannel channel = new(1, "Alice");
        channel.Receive(CreatePackets(0, 1)[0], 0d);
        channel.Receive(CreatePackets(0, 1)[1], 0.02d);

        float[] frame = new float[ModConstants.FrameSamples];
        Assert.False(channel.TryReadFrame(frame, 0.03d));

        channel.Receive(CreatePackets(2, 2)[0], 0.04d);
        Assert.True(channel.TryReadFrame(frame, 0.05d));
        Assert.True(channel.TryReadFrame(frame, 0.06d));
        Assert.True(channel.TryReadFrame(frame, 0.07d));

        // Packet 3 never arrives; packets 4 and 5 must produce concealed/decoded frames.
        channel.Receive(CreatePackets(4, 5)[0], 0.10d);
        channel.Receive(CreatePackets(4, 5)[1], 0.12d);

        Assert.Equal(3, channel.BufferedFrames);
        Assert.True(channel.TryReadFrame(frame, 0.10d));
        Assert.True(channel.TryReadFrame(frame, 0.12d));
        Assert.True(channel.TryReadFrame(frame, 0.14d));
    }

    [Fact]
    public void LatePacket_IsDropped()
    {
        using SpeakerChannel channel = new(1, "Alice");
        VoicePacket[] packets = CreatePackets(0, 3);

        channel.Receive(packets[0], 0.02d);
        channel.Receive(packets[2], 0.06d);
        channel.Receive(packets[1], 0.08d); // late

        Assert.Equal(3, channel.BufferedFrames); // 1, 2 and one concealed frame for sequence 1
    }

    [Fact]
    public void SpeakingState_HoldsForShortWindow()
    {
        using SpeakerChannel channel = new(1, "Alice");
        channel.Receive(CreatePackets(0, 0)[0], 10d);

        Assert.True(channel.IsSpeaking(10.2d));
        Assert.False(channel.IsSpeaking(11.0d));
    }

    [Fact]
    public void DecodeFailure_DoesNotStopLaterFrames()
    {
        using SpeakerChannel channel = new(1, "Alice");
        VoicePacket[] valid = CreatePackets(0, 1);
        channel.Receive(valid[0], 0d);
        channel.Receive(valid[1], 0.02d);

        VoicePacket broken = new(VoicePacket.CurrentVersion, 2, 10, "Town", new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
        Assert.False(channel.Receive(broken, 0.04d));

        channel.Receive(CreatePackets(3, 3)[0], 0.06d);

        float[] frame = new float[ModConstants.FrameSamples];
        Assert.True(channel.TryReadFrame(frame, 0.08d));
        Assert.True(channel.TryReadFrame(frame, 0.10d));
        Assert.True(channel.TryReadFrame(frame, 0.12d));
    }

    [Fact]
    public void Receive_CanSuppressAudioWhileTrackingSpeakingState()
    {
        using SpeakerChannel channel = new(1, "Alice");

        Assert.True(channel.Receive(CreatePackets(0, 0)[0], 10d, enqueueAudio: false));
        Assert.True(channel.IsSpeaking(10.2d));
        Assert.Equal(0, channel.BufferedFrames);
    }

    [Fact]
    public void DiscardBufferedAudio_KeepsSpeakingState()
    {
        using SpeakerChannel channel = new(1, "Alice");
        channel.Receive(CreatePackets(0, 0)[0], 10d);

        Assert.Equal(1, channel.BufferedFrames);
        channel.DiscardBufferedAudio();

        Assert.Equal(0, channel.BufferedFrames);
        Assert.True(channel.IsSpeaking(10.2d));
    }

    [Fact]
    public void Receive_StoresDeclaredRadiusPerSpeaker()
    {
        using SpeakerChannel alice = new(1, "Alice");
        using SpeakerChannel bob = new(2, "Bob");

        alice.Receive(CreatePackets(0, 0, radius: 3)[0], 0d);
        bob.Receive(CreatePackets(0, 0, radius: 20)[0], 0d);

        Assert.Equal(3, alice.ProximityRadius);
        Assert.Equal(20, bob.ProximityRadius);

        alice.Receive(CreatePackets(1, 1, radius: 10)[0], 0.02d);
        Assert.Equal(10, alice.ProximityRadius);
    }

    private static VoicePacket[] CreatePackets(ulong start, ulong end, int radius = 10)
    {
        VoicePacket[] packets = new VoicePacket[end - start + 1];
        using VoiceEncoder encoder = new();
        byte[] output = new byte[VoicePacket.MaxAudioBytes];
        float[] signal = new float[ModConstants.FrameSamples];

        for (int i = 0; i < signal.Length; i++)
            signal[i] = 0.2f * MathF.Sin(2f * MathF.PI * 300f * i / ModConstants.SampleRate);

        int length = encoder.Encode(signal, output);
        for (ulong sequence = start; sequence <= end; sequence++)
        {
            byte[] audio = new byte[length];
            Array.Copy(output, audio, length);
            packets[sequence - start] = new VoicePacket(VoicePacket.CurrentVersion, sequence, radius, "Town", audio);
        }

        return packets;
    }
}
