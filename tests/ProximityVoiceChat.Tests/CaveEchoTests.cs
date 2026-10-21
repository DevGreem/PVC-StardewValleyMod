namespace ProximityVoiceChat.Tests;

using System;
using ProximityVoiceChat.Audio;
using Xunit;

public class CaveEchoTests
{
    [Fact]
    public void Impulse_ProducesADelayedEcho()
    {
        CaveEcho echo = new(sampleRate: 1000);
        float[] samples = new float[echo.DelaySamples + 1];
        samples[0] = 1f;

        echo.Process(samples);

        Assert.Equal(1f, samples[0]);
        for (int i = 1; i < echo.DelaySamples; i++)
            Assert.Equal(0f, samples[i]);

        Assert.True(samples[echo.DelaySamples] > 0.1f);
    }

    [Fact]
    public void Reset_ClearsTheTail()
    {
        CaveEcho echo = new(sampleRate: 1000);
        float[] impulse = new float[echo.DelaySamples + 1];
        impulse[0] = 1f;
        echo.Process(impulse);

        echo.Reset();

        float[] silence = new float[echo.DelaySamples + 1];
        echo.Process(silence);
        Assert.All(silence, sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public void Feedback_DecaysOverTime()
    {
        CaveEcho echo = new(sampleRate: 1000);
        float[] impulse = new float[echo.DelaySamples + 1];
        impulse[0] = 1f;
        echo.Process(impulse);

        float previous = float.MaxValue;
        for (int pass = 0; pass < 4; pass++)
        {
            float[] tail = new float[echo.DelaySamples + 1];
            echo.Process(tail);
            float peak = 0f;
            foreach (float sample in tail)
                peak = MathF.Max(peak, MathF.Abs(sample));

            Assert.True(peak > 0f);
            Assert.True(peak < previous);
            previous = peak;
        }
    }

    [Fact]
    public void Process_KeepsOutputFinite()
    {
        CaveEcho echo = new(sampleRate: 1000);
        float[] samples = new float[echo.DelaySamples + 1];
        samples[0] = 1f;

        for (int pass = 0; pass < 100; pass++)
            echo.Process(samples);

        Assert.All(samples, sample => Assert.True(float.IsFinite(sample)));
    }
}
