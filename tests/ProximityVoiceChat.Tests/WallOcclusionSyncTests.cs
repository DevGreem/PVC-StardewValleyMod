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
    public void ShouldReplyToRequest_OnlyTheHostAnswersOtherPlayers()
    {
        Assert.True(WallOcclusionSync.ShouldReplyToRequest(
            isMainPlayer: true,
            fromPlayerId: 7,
            localPlayerId: 42,
            fromModId: ModConstants.ModId,
            type: ModConstants.WallOcclusionMessageType,
            request: true));

        Assert.False(WallOcclusionSync.ShouldReplyToRequest(
            isMainPlayer: true,
            fromPlayerId: 7,
            localPlayerId: 42,
            fromModId: ModConstants.ModId,
            type: ModConstants.WallOcclusionMessageType,
            request: false));

        Assert.False(WallOcclusionSync.ShouldReplyToRequest(
            isMainPlayer: false,
            fromPlayerId: 7,
            localPlayerId: 42,
            fromModId: ModConstants.ModId,
            type: ModConstants.WallOcclusionMessageType,
            request: true));

        Assert.False(WallOcclusionSync.ShouldReplyToRequest(
            isMainPlayer: true,
            fromPlayerId: 42,
            localPlayerId: 42,
            fromModId: ModConstants.ModId,
            type: ModConstants.WallOcclusionMessageType,
            request: true));
    }

    [Fact]
    public void IsSettingsMessage_RequiresTheModAndMessageType()
    {
        Assert.True(WallOcclusionSync.IsSettingsMessage(ModConstants.ModId, ModConstants.WallOcclusionMessageType));
        Assert.False(WallOcclusionSync.IsSettingsMessage("Someone.Else", ModConstants.WallOcclusionMessageType));
        Assert.False(WallOcclusionSync.IsSettingsMessage(ModConstants.ModId, "voice"));
        Assert.False(WallOcclusionSync.IsSettingsMessage(null, null));
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
