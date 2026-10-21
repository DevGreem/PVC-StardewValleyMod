namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat;
using ProximityVoiceChat.Audio;
using Xunit;

public class LocalMonitorSourceTests
{
    [Fact]
    public void AppendSamples_UpdatesLevelForCompleteFrame()
    {
        LocalMonitorSource source = new();
        source.AppendSamples(CreateSine(0.5f), ModConstants.FrameSamples);

        Assert.InRange(source.Level, 0.3f, 0.4f);
    }

    [Fact]
    public void MonitoringDisabled_DoesNotQueueFrames()
    {
        LocalMonitorSource source = new();
        source.MonitoringEnabled = false;
        source.AppendSamples(CreateSine(0.5f), ModConstants.FrameSamples);

        float[] output = new float[ModConstants.FrameSamples];
        Assert.False(source.TryReadFrame(output, 0d));
    }

    [Fact]
    public void MonitoringEnabled_QueuesAndReturnsCapturedFrame()
    {
        LocalMonitorSource source = new();
        source.MonitoringEnabled = true;
        source.AppendSamples(CreateSine(0.5f), ModConstants.FrameSamples);

        float[] output = new float[ModConstants.FrameSamples];
        Assert.True(source.TryReadFrame(output, 0d));
        Assert.True(Peak(output) > 0.4f);
    }

    [Fact]
    public void Clear_ResetsLevelAndQueue()
    {
        LocalMonitorSource source = new();
        source.MonitoringEnabled = true;
        source.AppendSamples(CreateSine(0.5f), ModConstants.FrameSamples);

        source.Clear();

        Assert.Equal(0f, source.Level);
        Assert.False(source.TryReadFrame(new float[ModConstants.FrameSamples], 0d));
    }

    [Fact]
    public void PlaybackMixer_AppliesMasterVolumeToLocalMonitor()
    {
        LocalMonitorSource source = new();
        source.MonitoringEnabled = true;
        source.AppendSamples(CreateSine(0.5f), ModConstants.FrameSamples);
        source.AppendSamples(CreateSine(0.5f), ModConstants.FrameSamples);

        PlaybackMixer mixer = new();
        mixer.SetSources(new IAudioFrameSource[] { source });
        mixer.SetMasterVolume(1f);

        float[] output = new float[ModConstants.FrameSamples];
        mixer.Mix(output);
        float fullPeak = Peak(output);
        Assert.InRange(fullPeak, 0.4f, 0.6f);

        mixer.SetMasterVolume(0.5f);
        mixer.Mix(output);
        Assert.InRange(Peak(output), fullPeak * 0.45f, fullPeak * 0.55f);
    }

    private static float[] CreateSine(float amplitude)
    {
        float[] samples = new float[ModConstants.FrameSamples];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = amplitude * MathF.Sin(2f * MathF.PI * 220f * i / ModConstants.SampleRate);

        return samples;
    }

    private static float Peak(float[] samples)
    {
        float peak = 0f;
        foreach (float sample in samples)
            peak = MathF.Max(peak, MathF.Abs(sample));

        return peak;
    }
}
