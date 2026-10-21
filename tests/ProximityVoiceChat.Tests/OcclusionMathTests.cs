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
    public void Gain_MatchesTheObstacleProgression(int obstacles, float expected)
    {
        Assert.Equal(expected, OcclusionMath.Gain(obstacles), 3);
    }

    [Fact]
    public void Gain_IsMonotonic()
    {
        float previous = OcclusionMath.Gain(0);
        for (int obstacles = 1; obstacles <= 3; obstacles++)
        {
            float current = OcclusionMath.Gain(obstacles);
            Assert.True(current < previous, $"gain at {obstacles} was {current}, previous {previous}");
            previous = current;
        }
    }

    [Theory]
    [InlineData(0, 1f)]
    [InlineData(1, 0.55f)]
    [InlineData(2, 0.10f)]
    [InlineData(3, 0.08f)]
    [InlineData(10, 0.08f)]
    public void LowPassAlpha_MatchesTheObstacleProgression(int obstacles, float expected)
    {
        Assert.Equal(expected, OcclusionMath.LowPassAlpha(obstacles), 3);
    }

    [Fact]
    public void LowPassAlpha_NeverDropsBelowItsFloor()
    {
        for (int obstacles = 0; obstacles < 20; obstacles++)
            Assert.True(OcclusionMath.LowPassAlpha(obstacles) >= 0.08f);
    }

    [Fact]
    public void IsSilent_StartsAtThreeObstacles()
    {
        Assert.False(OcclusionMath.IsSilent(0));
        Assert.False(OcclusionMath.IsSilent(1));
        Assert.False(OcclusionMath.IsSilent(2));
        Assert.True(OcclusionMath.IsSilent(3));
        Assert.True(OcclusionMath.IsSilent(4));
    }
}
