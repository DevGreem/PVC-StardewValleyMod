namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat;
using ProximityVoiceChat.Network;
using Xunit;

public class WallOcclusionSyncTests
{
    [Fact]
    public void ShouldApplyFromHost_AcceptsOnlyHostMessagesForClients()
    {
        Assert.True(WallOcclusionSync.ShouldApplyFromHost(
            isMainPlayer: false,
            fromPlayerId: 42,
            hostPlayerId: 42,
            fromModId: ModConstants.ModId,
            type: ModConstants.WallOcclusionMessageType));
    }

    [Fact]
    public void ShouldApplyFromHost_RejectsTheHostAndOtherSenders()
    {
        Assert.False(WallOcclusionSync.ShouldApplyFromHost(
            isMainPlayer: true,
            fromPlayerId: 42,
            hostPlayerId: 42,
            fromModId: ModConstants.ModId,
            type: ModConstants.WallOcclusionMessageType));

        Assert.False(WallOcclusionSync.ShouldApplyFromHost(
            isMainPlayer: false,
            fromPlayerId: 7,
            hostPlayerId: 42,
            fromModId: ModConstants.ModId,
            type: ModConstants.WallOcclusionMessageType));

        Assert.False(WallOcclusionSync.ShouldApplyFromHost(
            isMainPlayer: false,
            fromPlayerId: 42,
            hostPlayerId: 0,
            fromModId: ModConstants.ModId,
            type: ModConstants.WallOcclusionMessageType));

        Assert.False(WallOcclusionSync.ShouldApplyFromHost(
            isMainPlayer: false,
            fromPlayerId: 42,
            hostPlayerId: 42,
            fromModId: "Someone.Else",
            type: ModConstants.WallOcclusionMessageType));

        Assert.False(WallOcclusionSync.ShouldApplyFromHost(
            isMainPlayer: false,
            fromPlayerId: 42,
            hostPlayerId: 42,
            fromModId: ModConstants.ModId,
            type: "voice"));
    }

    [Fact]
    public void ResolveEffective_HostUsesItsPersistedPreference()
    {
        Assert.False(WallOcclusionSync.ResolveEffective(
            isMainPlayer: true,
            hostPreference: false,
            hasHostValue: true,
            hostValue: true));
        Assert.True(WallOcclusionSync.ResolveEffective(
            isMainPlayer: true,
            hostPreference: true,
            hasHostValue: false,
            hostValue: false));
    }

    [Fact]
    public void ResolveEffective_ClientUsesTheHostValue()
    {
        Assert.False(WallOcclusionSync.ResolveEffective(
            isMainPlayer: false,
            hostPreference: true,
            hasHostValue: true,
            hostValue: false));

        Assert.True(WallOcclusionSync.ResolveEffective(
            isMainPlayer: false,
            hostPreference: false,
            hasHostValue: true,
            hostValue: true));
    }

    [Fact]
    public void ResolveEffective_ClientWithoutHostValueDefaultsToTrue()
    {
        Assert.True(WallOcclusionSync.ResolveEffective(
            isMainPlayer: false,
            hostPreference: false,
            hasHostValue: false,
            hostValue: false));
    }
}
