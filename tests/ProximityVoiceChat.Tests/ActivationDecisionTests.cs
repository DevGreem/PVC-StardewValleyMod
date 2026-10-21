namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Audio;
using ProximityVoiceChat.Config;
using Xunit;

public class ActivationDecisionTests
{
    [Fact]
    public void MicrophoneDisabled_NeverTransmits()
    {
        Assert.False(ActivationDecision.ShouldTransmit(false, true, ActivationMode.PushToTalk, true, 1f, 50));
        Assert.False(ActivationDecision.ShouldTransmit(false, true, ActivationMode.VoiceActivity, true, 1f, 50));
    }

    [Fact]
    public void OutsideMultiplayerWorld_NeverTransmits()
    {
        Assert.False(ActivationDecision.ShouldTransmit(true, false, ActivationMode.PushToTalk, true, 1f, 50));
        Assert.False(ActivationDecision.ShouldTransmit(true, false, ActivationMode.VoiceActivity, true, 1f, 50));
    }

    [Fact]
    public void PushToTalk_TransmitsOnlyWhileHeld()
    {
        Assert.True(ActivationDecision.ShouldTransmit(true, true, ActivationMode.PushToTalk, true, 0f, 50));
        Assert.False(ActivationDecision.ShouldTransmit(true, true, ActivationMode.PushToTalk, false, 1f, 50));
    }

    [Fact]
    public void VoiceActivity_TransmitsAboveAndBelowThreshold()
    {
        float threshold = ActivationDecision.ThresholdFromSensitivity(50);
        Assert.True(ActivationDecision.ShouldTransmit(true, true, ActivationMode.VoiceActivity, false, threshold + 0.001f, 50));
        Assert.False(ActivationDecision.ShouldTransmit(true, true, ActivationMode.VoiceActivity, false, threshold - 0.001f, 50));
    }

    [Fact]
    public void HigherSensitivity_LowersThreshold()
    {
        float low = ActivationDecision.ThresholdFromSensitivity(0);
        float medium = ActivationDecision.ThresholdFromSensitivity(50);
        float high = ActivationDecision.ThresholdFromSensitivity(100);

        Assert.True(low > medium);
        Assert.True(medium > high);
        Assert.True(high > 0f);
    }

    [Fact]
    public void Rms_OfSilenceIsZero()
    {
        Assert.Equal(0f, ActivationDecision.CalculateRms(new float[960]));
    }

    [Fact]
    public void Rms_OfFullScaleSquareWaveIsOne()
    {
        float[] samples = new float[100];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = i % 2 == 0 ? 1f : -1f;

        Assert.Equal(1f, ActivationDecision.CalculateRms(samples), 3);
    }
}
