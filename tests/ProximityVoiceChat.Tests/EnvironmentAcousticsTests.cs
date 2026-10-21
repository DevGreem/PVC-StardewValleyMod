namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Audio;
using Xunit;

public class EnvironmentAcousticsTests
{
    [Fact]
    public void ApplyGain_LeavesNormalLocationsUnchanged()
    {
        Assert.Equal(0.6f, EnvironmentAcoustics.ApplyGain(0.6f, inCave: false), 3);
    }

    [Fact]
    public void ApplyGain_ReducesCaveVolume()
    {
        Assert.Equal(0.80f, EnvironmentAcoustics.CaveGain, 3);
        Assert.Equal(0.6f * EnvironmentAcoustics.CaveGain, EnvironmentAcoustics.ApplyGain(0.6f, inCave: true), 3);
    }

    [Fact]
    public void ApplyLowPassAlpha_LeavesNormalLocationsUnchanged()
    {
        Assert.Equal(0.55f, EnvironmentAcoustics.ApplyLowPassAlpha(0.55f, inCave: false), 3);
    }

    [Fact]
    public void ApplyLowPassAlpha_CapsCaveMuffling()
    {
        Assert.Equal(0.55f, EnvironmentAcoustics.CaveLowPassAlpha, 3);
        Assert.Equal(EnvironmentAcoustics.CaveLowPassAlpha, EnvironmentAcoustics.ApplyLowPassAlpha(0.80f, inCave: true), 3);
        Assert.Equal(0.10f, EnvironmentAcoustics.ApplyLowPassAlpha(0.10f, inCave: true), 3);
    }
}
