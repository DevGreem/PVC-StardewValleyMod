namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.UI;
using Xunit;

public class PlayerMuteMenuModelTests
{
    private static readonly PlayerMuteCandidate[] Players =
    {
        new(1, "Alice", "Farm", 10, 10, false),
        new(2, "Bob", "Farm", 14, 10, true),
        new(3, "Carol", "Town", 10, 10, false),
        new(4, "Dave", "Farm", 30, 10, false)
    };

    [Fact]
    public void Nearby_OnlyIncludesSameLocationAndReach()
    {
        IReadOnlyList<PlayerMuteRow> rows = PlayerMuteMenuModel.Build(
            Players,
            localLocation: "Farm",
            localTileX: 10,
            localTileY: 10,
            nearbyOnly: true,
            reach: 10);

        Assert.Equal(new[] { "Alice", "Bob" }, rows.Select(row => row.Name));
        Assert.All(rows, row => Assert.Equal("Farm", row.Location));
    }

    [Fact]
    public void All_IncludesPlayersInEveryLocation()
    {
        IReadOnlyList<PlayerMuteRow> rows = PlayerMuteMenuModel.Build(
            Players,
            localLocation: "Farm",
            localTileX: 10,
            localTileY: 10,
            nearbyOnly: false,
            reach: 10);

        Assert.Equal(new[] { "Alice", "Bob", "Carol", "Dave" }, rows.Select(row => row.Name));
    }

    [Fact]
    public void RowsCarryMuteStateAndDistance()
    {
        IReadOnlyList<PlayerMuteRow> rows = PlayerMuteMenuModel.Build(
            Players,
            localLocation: "Farm",
            localTileX: 10,
            localTileY: 10,
            nearbyOnly: true,
            reach: 10);

        Assert.Equal(4f, rows[1].Distance, 3);
        Assert.True(rows[1].IsMuted);
        Assert.False(rows[0].IsMuted);
    }

    [Fact]
    public void Nearby_SortsByDistanceThenName()
    {
        PlayerMuteCandidate[] players =
        {
            new(1, "Zoe", "Farm", 15, 10, false),
            new(2, "Amy", "Farm", 10, 10, false),
            new(3, "Ben", "Farm", 15, 10, false)
        };

        IReadOnlyList<PlayerMuteRow> rows = PlayerMuteMenuModel.Build(
            players,
            localLocation: "Farm",
            localTileX: 10,
            localTileY: 10,
            nearbyOnly: true,
            reach: 20);

        Assert.Equal(new[] { "Amy", "Ben", "Zoe" }, rows.Select(row => row.Name));
    }
}
