namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat;
using ProximityVoiceChat.Network;
using Xunit;

public class VoiceTransportFilterTests
{
    [Fact]
    public void AcceptsVoiceMessagesFromOtherPlayersOfThisMod()
    {
        Assert.True(VoiceTransport.IsVoiceMessageForThisMod(ModConstants.ModId, ModConstants.VoiceMessageType, 2, 1));
    }

    [Fact]
    public void IgnoresMessagesFromOtherMods()
    {
        Assert.False(VoiceTransport.IsVoiceMessageForThisMod("Someone.Else", ModConstants.VoiceMessageType, 2, 1));
    }

    [Fact]
    public void IgnoresOtherMessageTypes()
    {
        Assert.False(VoiceTransport.IsVoiceMessageForThisMod(ModConstants.ModId, "something-else", 2, 1));
    }

    [Fact]
    public void IgnoresOwnEcho()
    {
        Assert.False(VoiceTransport.IsVoiceMessageForThisMod(ModConstants.ModId, ModConstants.VoiceMessageType, 7, 7));
    }
}
