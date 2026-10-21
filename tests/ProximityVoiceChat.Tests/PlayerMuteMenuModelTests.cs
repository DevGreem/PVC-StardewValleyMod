namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.UI;
using Xunit;

public class PlayerMuteMenuModelTests
{
    private static readonly PlayerMuteCandidate[] Players =
    {
        new(1, "Alice", "Farm", 10, 10, VoiceReach: 10, IsMuted: false),
        new(2, "Bob", "Farm", 14, 10, VoiceReach: 3, IsMuted: true),
        new(3, "Carol", "Town", 10, 10, VoiceReach: 10, IsMuted: false),
        new(4, "Dave", "Farm", 30, 10, VoiceReach: 10, IsMuted: false),
        new(5, "Eve", "Farm", 18, 10, VoiceReach: 20, IsMuted: false)
    };

    [Fact]
    public void Nearby_OnlyIncludesSameLocationAndEachPlayersReach()
    {
        IReadOnlyList<PlayerMuteRow> rows = PlayerMuteMenuModel.Build(
            Players,
            localLocation: "Farm",
            localTileX: 10,
            localTileY: 10,
            nearbyOnly: true);

        Assert.Equal(new[] { "Alice", "Eve" }, rows.Select(row => row.Name));
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
            nearbyOnly: false);

        Assert.Equal(new[] { "Alice", "Bob", "Carol", "Dave", "Eve" }, rows.Select(row => row.Name));
    }

    [Fact]
    public void RowsCarryMuteStateReachAndDistance()
    {
        IReadOnlyList<PlayerMuteRow> rows = PlayerMuteMenuModel.Build(
            Players,
            localLocation: "Farm",
            localTileX: 10,
            localTileY: 10,
            nearbyOnly: false);

        PlayerMuteRow bob = rows.Single(row => row.Name == "Bob");
        Assert.Equal(4f, bob.Distance, 3);
        Assert.Equal(3, bob.VoiceReach);
        Assert.True(bob.IsMuted);
        Assert.False(rows.Single(row => row.Name == "Alice").IsMuted);
    }

    [Fact]
    public void Nearby_SortsByDistanceThenName()
    {
        PlayerMuteCandidate[] players =
        {
            new(1, "Zoe", "Farm", 15, 10, VoiceReach: 20, IsMuted: false),
            new(2, "Amy", "Farm", 10, 10, VoiceReach: 20, IsMuted: false),
            new(3, "Ben", "Farm", 15, 10, VoiceReach: 20, IsMuted: false)
        };

        IReadOnlyList<PlayerMuteRow> rows = PlayerMuteMenuModel.Build(
            players,
            localLocation: "Farm",
            localTileX: 10,
            localTileY: 10,
            nearbyOnly: true);

        Assert.Equal(new[] { "Amy", "Ben", "Zoe" }, rows.Select(row => row.Name));
    }

    [Fact]
    public void Nearby_UsesTheLargerReachOfTheOtherPlayer()
    {
        PlayerMuteCandidate[] players =
        {
            new(1, "Nearby", "Farm", 10, 10, VoiceReach: 3, IsMuted: false),
            new(2, "ShortReach", "Farm", 15, 10, VoiceReach: 3, IsMuted: false),
            new(3, "FarButAudible", "Farm", 25, 10, VoiceReach: 20, IsMuted: false)
        };

        IReadOnlyList<PlayerMuteRow> rows = PlayerMuteMenuModel.Build(
            players,
            localLocation: "Farm",
            localTileX: 10,
            localTileY: 10,
            nearbyOnly: true);

        Assert.Equal(new[] { "Nearby", "FarButAudible" }, rows.Select(row => row.Name));
    }
}
