namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Audio;
using Xunit;

public class OnePoleLowPassTests
{
    [Fact]
    public void AlphaOne_IsPassthrough()
    {
        OnePoleLowPass filter = new();
        float[] samples = { 0.5f, -0.25f, 0.75f };

        filter.Process(samples, targetAlpha: 1f);

        Assert.Equal(0.5f, samples[0]);
        Assert.Equal(-0.25f, samples[1]);
        Assert.Equal(0.75f, samples[2]);
        Assert.Equal(1f, filter.Alpha);
    }

    [Fact]
    public void LowAlpha_AttenuatesHighFrequencyContent()
    {
        OnePoleLowPass filter = new();
        float[] samples = new float[128];

        for (int pass = 0; pass < 30; pass++)
        {
            for (int i = 0; i < samples.Length; i++)
                samples[i] = (i % 2 == 0) ? 1f : -1f;

            filter.Process(samples, targetAlpha: 0.08f);
        }

        Assert.True(filter.Alpha < 0.2f, $"alpha was {filter.Alpha}");
        Assert.All(samples, sample => Assert.InRange(sample, -0.2f, 0.2f));
    }

    [Fact]
    public void Reset_RestoresPassthroughAndClearsState()
    {
        OnePoleLowPass filter = new();
        float[] attenuated = { 1f, -1f, 1f, -1f };

        for (int i = 0; i < 10; i++)
            filter.Process(attenuated, targetAlpha: 0.08f);

        filter.Reset();

        Assert.Equal(1f, filter.Alpha);

        float[] passthrough = { 0.3f, -0.4f };
        filter.Process(passthrough, targetAlpha: 1f);
        Assert.Equal(0.3f, passthrough[0]);
        Assert.Equal(-0.4f, passthrough[1]);
    }
}
