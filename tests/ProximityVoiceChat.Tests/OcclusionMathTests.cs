namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Audio;
using Xunit;

public class OcclusionMathTests
{
    [Theory]
    [InlineData(0, 1f)]
    [InlineData(1, 0.6f)]
    [InlineData(2, 0.2f)]
    [InlineData(3, 0f)]
    [InlineData(4, 0f)]
    public void Gain_MatchesTheBlockedTileProgression(int blockedTiles, float expected)
    {
        Assert.Equal(expected, OcclusionMath.Gain(blockedTiles), 3);
    }

    [Fact]
    public void Gain_IsMonotonic()
    {
        float previous = OcclusionMath.Gain(0);
        for (int blocked = 1; blocked <= 3; blocked++)
        {
            float current = OcclusionMath.Gain(blocked);
            Assert.True(current < previous, $"gain at {blocked} was {current}, previous {previous}");
            previous = current;
        }
    }

    [Theory]
    [InlineData(0, 1f)]
    [InlineData(1, 0.55f)]
    [InlineData(2, 0.10f)]
    [InlineData(3, 0.08f)]
    [InlineData(10, 0.08f)]
    public void LowPassAlpha_MatchesTheBlockedTileProgression(int blockedTiles, float expected)
    {
        Assert.Equal(expected, OcclusionMath.LowPassAlpha(blockedTiles), 3);
    }

    [Fact]
    public void LowPassAlpha_NeverDropsBelowItsFloor()
    {
        for (int blocked = 0; blocked < 20; blocked++)
            Assert.True(OcclusionMath.LowPassAlpha(blocked) >= 0.08f);
    }

    [Fact]
    public void IsSilent_StartsAtThreeBlockedTiles()
    {
        Assert.False(OcclusionMath.IsSilent(0));
        Assert.False(OcclusionMath.IsSilent(1));
        Assert.False(OcclusionMath.IsSilent(2));
        Assert.True(OcclusionMath.IsSilent(3));
        Assert.True(OcclusionMath.IsSilent(4));
    }
}
