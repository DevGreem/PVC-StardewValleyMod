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
    public void IsNearbySpeaking_RequiresNearbyAndSpeakingState()
    {
        Assert.True(IndicatorRules.IsNearbySpeaking(true, true));
        Assert.False(IndicatorRules.IsNearbySpeaking(false, true));
        Assert.False(IndicatorRules.IsNearbySpeaking(true, false));
    }

    [Fact]
    public void IsNearbySpeaking_FollowsTheSpeakerDeclaredRadius()
    {
        bool insideSmall = ProximityMath.IsInRange(2f, 3f);
        bool outsideSmall = ProximityMath.IsInRange(3f, 3f);
        bool insideLarge = ProximityMath.IsInRange(6f, 10f);

        Assert.True(IndicatorRules.IsNearbySpeaking(insideSmall, true));
        Assert.False(IndicatorRules.IsNearbySpeaking(outsideSmall, true));
        Assert.True(IndicatorRules.IsNearbySpeaking(insideLarge, true));
    }

    [Fact]
    public void FormatVoiceReach_ShowsTheLocalOutgoingReach()
    {
        Assert.Equal("Voice reach: 10 tiles", IndicatorRules.FormatVoiceReach(10));
        Assert.Equal("Voice reach: 20 tiles", IndicatorRules.FormatVoiceReach(20));
    }
}
