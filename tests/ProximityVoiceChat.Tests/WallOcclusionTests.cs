namespace ProximityVoiceChat.Tests;

using System;
using System.Collections.Generic;
using ProximityVoiceChat.Audio;
using Xunit;

public class WallOcclusionTests
{
    [Fact]
    public void CountBlockingTiles_ClearLine_ReturnsZero()
    {
        int blocked = WallOcclusion.CountBlockingTiles(0, 2, 4, 2, Grid(5, 5));

        Assert.Equal(0, blocked);
    }

    [Fact]
    public void CountBlockingTiles_CountsOneWall()
    {
        int blocked = WallOcclusion.CountBlockingTiles(0, 2, 4, 2, Grid(5, 5, (2, 2)));

        Assert.Equal(1, blocked);
    }

    [Fact]
    public void CountBlockingTiles_CountsATwoTileThickWall()
    {
        int blocked = WallOcclusion.CountBlockingTiles(0, 2, 4, 2, Grid(5, 5, (2, 2), (3, 2)));

        Assert.Equal(2, blocked);
    }

    [Fact]
    public void CountBlockingTiles_SkipsTheEndpoints()
    {
        int blocked = WallOcclusion.CountBlockingTiles(0, 2, 4, 2, Grid(5, 5, (0, 2), (4, 2)));

        Assert.Equal(0, blocked);
    }

    [Fact]
    public void CountBlockingTiles_CountsADiagonalWall()
    {
        int blocked = WallOcclusion.CountBlockingTiles(0, 0, 3, 3, Grid(4, 4, (1, 1), (2, 2)));

        Assert.Equal(2, blocked);
    }

    [Fact]
    public void CountBlockingTiles_OutOfBoundsTilesDoNotThrow()
    {
        int blocked = WallOcclusion.CountBlockingTiles(
            -4,
            2,
            8,
            2,
            (x, y) => x >= 0 && y >= 0 && x < 5 && y < 5 && x == 2 && y == 2);

        Assert.Equal(1, blocked);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("T", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("F", false)]
    [InlineData("false", false)]
    public void IsPassable_ReadsThePropertyValue(string? value, bool expected)
    {
        Assert.Equal(expected, WallOcclusion.IsPassable(value));
    }

    [Fact]
    public void IsPassable_AbsentPropertyIsNotPassable()
    {
        Assert.False(WallOcclusion.IsPassable(null));
    }

    private static Func<int, int, bool> Grid(int width, int height, params (int X, int Y)[] walls)
    {
        HashSet<(int X, int Y)> set = new(walls);
        return (x, y) => x >= 0 && y >= 0 && x < width && y < height && set.Contains((x, y));
    }
}
