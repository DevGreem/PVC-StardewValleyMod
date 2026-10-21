namespace ProximityVoiceChat.Tests;

using ProximityVoiceChat.Audio;
using ProximityVoiceChat.UI;
using Xunit;

public class IndicatorRulesTests
{
    [Fact]
    public void ShouldDraw_RequiresEnabledWorldAndMultiplayer()
    {
        Assert.True(IndicatorRules.ShouldDraw(true, true, true));
        Assert.False(IndicatorRules.ShouldDraw(false, true, true));
        Assert.False(IndicatorRules.ShouldDraw(true, false, true));
        Assert.False(IndicatorRules.ShouldDraw(true, true, false));
    }

    [Fact]
    public void IsNearbySpeakerVisible_RequiresNearbyAndEitherSpeakingOrMuted()
    {
        Assert.True(IndicatorRules.IsNearbySpeakerVisible(true, isSpeaking: true, isMuted: false));
        Assert.True(IndicatorRules.IsNearbySpeakerVisible(true, isSpeaking: false, isMuted: true));
        Assert.False(IndicatorRules.IsNearbySpeakerVisible(true, isSpeaking: false, isMuted: false));
        Assert.False(IndicatorRules.IsNearbySpeakerVisible(false, isSpeaking: true, isMuted: false));
        Assert.False(IndicatorRules.IsNearbySpeakerVisible(false, isSpeaking: false, isMuted: true));
    }

    [Fact]
    public void IsNearbySpeakerVisible_FollowsTheSpeakerDeclaredRadius()
    {
        bool insideSmall = ProximityMath.IsInRange(2f, 3f);
        bool outsideSmall = ProximityMath.IsInRange(3f, 3f);
        bool insideLarge = ProximityMath.IsInRange(6f, 10f);

        Assert.True(IndicatorRules.IsNearbySpeakerVisible(insideSmall, isSpeaking: true, isMuted: false));
        Assert.False(IndicatorRules.IsNearbySpeakerVisible(outsideSmall, isSpeaking: true, isMuted: false));
        Assert.True(IndicatorRules.IsNearbySpeakerVisible(insideLarge, isSpeaking: true, isMuted: false));
        Assert.False(IndicatorRules.IsNearbySpeakerVisible(outsideSmall, isSpeaking: false, isMuted: true));
    }

    [Fact]
    public void FormatVoiceReach_ShowsTheLocalOutgoingReach()
    {
        Assert.Equal("Voice reach: 10 tiles", IndicatorRules.FormatVoiceReach(10));
        Assert.Equal("Voice reach: 20 tiles", IndicatorRules.FormatVoiceReach(20));
    }

    [Fact]
    public void FormatNearbyOverflow_HandlesZeroOneAndSeveral()
    {
        Assert.Equal("", IndicatorRules.FormatNearbyOverflow(0));
        Assert.Equal("", IndicatorRules.FormatNearbyOverflow(-1));
        Assert.Equal("+1 player nearby", IndicatorRules.FormatNearbyOverflow(1));
        Assert.Equal("+2 players nearby", IndicatorRules.FormatNearbyOverflow(2));
        Assert.Equal("+5 players nearby", IndicatorRules.FormatNearbyOverflow(5));
    }

    [Fact]
    public void MaxVisibleNearbyPlayers_LeavesRoomForTheSummary()
    {
        Assert.Equal(7, IndicatorRules.MaxVisibleNearbyPlayers);
    }

    [Fact]
    public void VisibleAndHiddenCounts_SplitAtSeven()
    {
        Assert.Equal(7, IndicatorRules.GetVisibleCount(7));
        Assert.Equal(0, IndicatorRules.GetHiddenCount(7));
        Assert.Equal(7, IndicatorRules.GetVisibleCount(8));
        Assert.Equal(1, IndicatorRules.GetHiddenCount(8));
        Assert.Equal(7, IndicatorRules.GetVisibleCount(9));
        Assert.Equal(2, IndicatorRules.GetHiddenCount(9));
        Assert.Equal(7, IndicatorRules.GetVisibleCount(12));
        Assert.Equal(5, IndicatorRules.GetHiddenCount(12));
    }
}
