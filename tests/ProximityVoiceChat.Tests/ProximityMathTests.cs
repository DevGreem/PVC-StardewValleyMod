namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Audio;
using Xunit;

public class ProximityMathTests
{
    [Fact]
    public void Volume_IsFullAtZeroDistance()
    {
        Assert.Equal(1f, ProximityMath.Volume(0f, 10f));
    }

    [Fact]
    public void Volume_IsSilentAtAndBeyondRadius()
    {
        Assert.Equal(0f, ProximityMath.Volume(10f, 10f));
        Assert.Equal(0f, ProximityMath.Volume(25f, 10f));
    }

    [Fact]
    public void Volume_DecreasesMonotonicallyBetweenZeroAndRadius()
    {
        float previous = 1f;
        for (float distance = 0.5f; distance < 10f; distance += 0.5f)
        {
            float current = ProximityMath.Volume(distance, 10f);
            Assert.True(current < previous, $"volume at {distance} was {current}, previous {previous}");
            Assert.InRange(current, 0f, 1f);
            previous = current;
        }
    }

    [Fact]
    public void CalculateVolume_IsSilentAcrossLocations()
    {
        Assert.Equal(0f, ProximityMath.CalculateVolume("Farm", "Cabin1", 0f, 10f));
        Assert.Equal(1f, ProximityMath.CalculateVolume("Farm", "Farm", 0f, 10f));
    }

    [Fact]
    public void CalculateVolume_GoesSilentWhenSpeakerLeavesRadius()
    {
        Assert.True(ProximityMath.CalculateVolume("Farm", "Farm", 2f, 10f) > 0f);
        Assert.Equal(0f, ProximityMath.CalculateVolume("Farm", "Farm", 10f, 10f));
        Assert.Equal(0f, ProximityMath.CalculateVolume("Farm", "Farm", 20f, 10f));
    }

    [Fact]
    public void CalculateVolume_UsesSpeakerDeclaredRadius()
    {
        // The listener's own configured radius is not an input here: the speaker's
        // declared reach decides both audibility and the distance falloff.
        Assert.True(ProximityMath.CalculateVolume("Farm", "Farm", 2f, 3f) > 0f);
        Assert.Equal(0f, ProximityMath.CalculateVolume("Farm", "Farm", 3f, 3f));
        Assert.True(ProximityMath.CalculateVolume("Farm", "Farm", 6f, 10f) > 0f);
        Assert.Equal(0f, ProximityMath.CalculateVolume("Farm", "Farm", 10f, 10f));
        Assert.True(ProximityMath.CalculateVolume("Farm", "Farm", 19f, 20f) > 0f);
        Assert.Equal(0f, ProximityMath.CalculateVolume("Farm", "Farm", 20f, 20f));
    }

    [Fact]
    public void CalculateVolume_ReturnsDistanceGainWithoutMasterVolume()
    {
        Assert.Equal(1f, ProximityMath.CalculateVolume("Farm", "Farm", 0f, 10f));
        Assert.Equal(0.5f, ProximityMath.CalculateVolume("Farm", "Farm", 5f, 10f), 3);
    }

    [Fact]
    public void SameLocation_RequiresNonEmptyAndEqualValues()
    {
        Assert.True(ProximityMath.IsSameLocation("Town", "Town"));
        Assert.False(ProximityMath.IsSameLocation("Town", "Farm"));
        Assert.False(ProximityMath.IsSameLocation("", ""));
        Assert.False(ProximityMath.IsSameLocation(null, "Town"));
    }
}
