namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Network;
using Xunit;

public class VoiceRecipientRulesTests
{
    [Fact]
    public void Select_OnlyIncludesSameLocationAndInRangePeers()
    {
        VoiceRecipientCandidate[] candidates =
        {
            new(1, "Farm", 12, 10),
            new(2, "Farm", 14, 10),
            new(3, "Town", 10, 10),
            new(4, "Farm", 10, 10)
        };

        long[] recipients = VoiceRecipientRules.Select("Farm", 10, 10, 3, candidates);

        Assert.Equal(new long[] { 1, 4 }, recipients);
    }

    [Fact]
    public void Select_ReturnsEmptyWhenNoPeerIsEligible()
    {
        VoiceRecipientCandidate[] candidates =
        {
            new(1, "Town", 10, 10),
            new(2, "Farm", 20, 10)
        };

        Assert.Empty(VoiceRecipientRules.Select("Farm", 10, 10, 3, candidates));
    }

    [Fact]
    public void Select_ReturnsEmptyWithoutALocationOrRadius()
    {
        VoiceRecipientCandidate[] candidates = { new(1, "Farm", 10, 10) };

        Assert.Empty(VoiceRecipientRules.Select("", 10, 10, 3, candidates));
        Assert.Empty(VoiceRecipientRules.Select("Farm", 10, 10, 0, candidates));
    }

    [Fact]
    public void IsKnownFarmer_MatchesCurrentIdsAndTheHost()
    {
        long[] ids = { 10, 20 };

        Assert.True(VoiceTransport.IsKnownFarmer(10, ids));
        Assert.True(VoiceTransport.IsKnownFarmer(30, ids, hostPlayerId: 30));
        Assert.False(VoiceTransport.IsKnownFarmer(40, ids, hostPlayerId: 30));
    }
}
