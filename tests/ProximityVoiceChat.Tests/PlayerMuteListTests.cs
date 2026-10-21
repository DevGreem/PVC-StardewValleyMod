namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Config;
using Xunit;

public class PlayerMuteListTests
{
    [Fact]
    public void Rebuild_LoadsValidEntries()
    {
        ModConfig config = new()
        {
            MutedPlayers =
            {
                new MutedPlayer { PlayerId = 10, PlayerName = "Alice" },
                new MutedPlayer { PlayerId = 20, PlayerName = "Bob" }
            }
        };
        PlayerMuteList mutes = new();

        mutes.Rebuild(config);

        Assert.True(mutes.IsMuted(10));
        Assert.True(mutes.IsMuted(20));
        Assert.False(mutes.IsMuted(30));
    }

    [Fact]
    public void Toggle_AddsAndRemovesFromConfig()
    {
        ModConfig config = new();
        PlayerMuteList mutes = new();

        Assert.True(mutes.Toggle(config, 10, "Alice"));
        Assert.True(mutes.IsMuted(10));
        Assert.Single(config.MutedPlayers);
        Assert.Equal("Alice", config.MutedPlayers[0].PlayerName);

        Assert.False(mutes.Toggle(config, 10, "Alice"));
        Assert.False(mutes.IsMuted(10));
        Assert.Empty(config.MutedPlayers);
    }
}
