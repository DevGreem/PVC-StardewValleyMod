namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat;
using ProximityVoiceChat.Audio;
using ProximityVoiceChat.Config;
using ProximityVoiceChat.Network;
using Xunit;

public class CapturePipelineTests
{
    [Fact]
    public void PushToTalk_ProducesOnePacketPerTwentyMilliseconds()
    {
        List<VoicePacket> sent = new();
        using CapturePipeline pipeline = new(sent.Add);
        pipeline.MicrophoneEnabled = true;
        pipeline.SessionActive = true;
        pipeline.LocationName = "Town";
        pipeline.ActivationMode = ActivationMode.PushToTalk;
        pipeline.PushToTalkHeld = true;
        pipeline.ProximityRadius = 10;

        float[] chunk = new float[480]; // 10 ms
        for (int i = 0; i < 100; i++)
            pipeline.AppendSamples(chunk, chunk.Length);

        Assert.Equal(50, pipeline.FramesFilled);
        Assert.Equal(50, pipeline.FramesTransmitted);
        Assert.Equal(50, sent.Count);
        Assert.True(pipeline.IsTransmittingNow);

        for (int i = 0; i < sent.Count; i++)
        {
            Assert.Equal((ulong)i, sent[i].Sequence);
            Assert.Equal("Town", sent[i].LocationName);
            Assert.Equal(10, sent[i].ProximityRadius);
        }
    }

    [Fact]
    public void VoiceActivity_DoesNotTransmitSilence()
    {
        List<VoicePacket> sent = new();
        using CapturePipeline pipeline = new(sent.Add);
        pipeline.MicrophoneEnabled = true;
        pipeline.SessionActive = true;
        pipeline.LocationName = "Town";
        pipeline.ActivationMode = ActivationMode.VoiceActivity;
        pipeline.Sensitivity = 50;

        float[] silence = new float[ModConstants.FrameSamples];
        for (int i = 0; i < 50; i++)
            pipeline.AppendSamples(silence, silence.Length);

        Assert.Equal(50, pipeline.FramesFilled);
        Assert.Equal(0, pipeline.FramesTransmitted);
        Assert.False(pipeline.IsTransmittingNow);
        Assert.Empty(sent);
    }

    [Fact]
    public void DisabledMicrophone_CapturesNothing()
    {
        List<VoicePacket> sent = new();
        using CapturePipeline pipeline = new(sent.Add);
        pipeline.MicrophoneEnabled = false;
        pipeline.SessionActive = true;
        pipeline.LocationName = "Town";
        pipeline.ActivationMode = ActivationMode.PushToTalk;
        pipeline.PushToTalkHeld = true;

        float[] chunk = new float[ModConstants.FrameSamples];
        pipeline.AppendSamples(chunk, chunk.Length);

        Assert.Equal(0, pipeline.FramesFilled);
        Assert.Empty(sent);
    }
}
