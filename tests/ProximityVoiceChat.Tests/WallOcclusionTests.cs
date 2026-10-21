namespace ProximityVoiceChat.Tests;

using System;
using System.Collections.Generic;
using ProximityVoiceChat.Audio;
using Xunit;

public class WallOcclusionTests
{
    private const int MapWall = -1;
    private const int Cabin = 10;
    private const int Shed = 11;

    [Fact]
    public void CountObstacles_ClearLine_ReturnsZero()
    {
        int obstacles = WallOcclusion.CountObstacles(0, 2, 4, 2, Grid(5, 5));

        Assert.Equal(0, obstacles);
    }

    [Fact]
    public void CountObstacles_OneMapWall_ReturnsOne()
    {
        int obstacles = WallOcclusion.CountObstacles(0, 2, 4, 2, Grid(5, 5, (2, 2, MapWall)));

        Assert.Equal(1, obstacles);
    }

    [Fact]
    public void CountObstacles_ThickMapWall_ReturnsOne()
    {
        int obstacles = WallOcclusion.CountObstacles(
            0,
            2,
            5,
            2,
            Grid(6, 5, (2, 2, MapWall), (3, 2, MapWall), (4, 2, MapWall)));

        Assert.Equal(1, obstacles);
    }

    [Fact]
    public void CountObstacles_TwoSeparatedMapWalls_ReturnsTwo()
    {
        int obstacles = WallOcclusion.CountObstacles(
            0,
            2,
            6,
            2,
            Grid(7, 5, (2, 2, MapWall), (4, 2, MapWall)));

        Assert.Equal(2, obstacles);
    }

    [Fact]
    public void CountObstacles_OneBuildingFootprint_ReturnsOne()
    {
        int obstacles = WallOcclusion.CountObstacles(
            0,
            2,
            8,
            2,
            Grid(9, 5, (2, 2, Cabin), (3, 2, Cabin), (4, 2, Cabin), (5, 2, Cabin), (6, 2, Cabin)));

        Assert.Equal(1, obstacles);
    }

    [Fact]
    public void CountObstacles_TwoAdjacentBuildings_ReturnsTwo()
    {
        int obstacles = WallOcclusion.CountObstacles(
            0,
            2,
            8,
            2,
            Grid(9, 5, (2, 2, Cabin), (3, 2, Cabin), (5, 2, Shed), (6, 2, Shed)));

        Assert.Equal(2, obstacles);
    }

    [Fact]
    public void CountObstacles_BuildingAndWall_ReturnsTwo()
    {
        int obstacles = WallOcclusion.CountObstacles(
            0,
            2,
            8,
            2,
            Grid(9, 5, (2, 2, Cabin), (3, 2, Cabin), (6, 2, MapWall)));

        Assert.Equal(2, obstacles);
    }

    [Fact]
    public void CountObstacles_ThreeObstacles_AreSilent()
    {
        int obstacles = WallOcclusion.CountObstacles(
            0,
            2,
            12,
            2,
            Grid(13, 5, (2, 2, MapWall), (5, 2, Cabin), (6, 2, Cabin), (9, 2, MapWall)));

        Assert.Equal(3, obstacles);
        Assert.True(OcclusionMath.IsSilent(obstacles));
    }

    [Fact]
    public void CountObstacles_SkipsTheEndpoints()
    {
        int obstacles = WallOcclusion.CountObstacles(
            0,
            2,
            4,
            2,
            Grid(5, 5, (0, 2, MapWall), (4, 2, MapWall)));

        Assert.Equal(0, obstacles);
    }

    [Fact]
    public void CountObstacles_DiagonalWall_CountsOneRun()
    {
        int obstacles = WallOcclusion.CountObstacles(
            0,
            0,
            3,
            3,
            Grid(4, 4, (1, 1, MapWall), (2, 2, MapWall)));

        Assert.Equal(1, obstacles);
    }

    [Fact]
    public void CountObstacles_OutOfBoundsTilesDoNotThrow()
    {
        int obstacles = WallOcclusion.CountObstacles(
            -4,
            2,
            8,
            2,
            (x, y) => x >= 0 && y >= 0 && x < 5 && y < 5 && x == 2 && y == 2 ? MapWall : 0);

        Assert.Equal(1, obstacles);
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

    private static Func<int, int, int> Grid(int width, int height, params (int X, int Y, int Id)[] tiles)
    {
        Dictionary<(int X, int Y), int> map = new();
        foreach ((int x, int y, int id) in tiles)
            map[(x, y)] = id;

        return (x, y) => x >= 0 && y >= 0 && x < width && y < height && map.TryGetValue((x, y), out int id)
            ? id
            : 0;
    }
}
