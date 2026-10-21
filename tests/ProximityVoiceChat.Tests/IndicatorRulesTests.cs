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
    public void IsNearbySpeaking_RequiresAudibleVolumeAndSpeakingState()
    {
        Assert.True(IndicatorRules.IsNearbySpeaking(0.5f, true));
        Assert.False(IndicatorRules.IsNearbySpeaking(0f, true));
        Assert.False(IndicatorRules.IsNearbySpeaking(0.5f, false));
    }

    [Fact]
    public void IsNearbySpeaking_FollowsTheSpeakerDeclaredRadius()
    {
        float insideSmall = ProximityMath.CalculateVolume("Farm", "Farm", 2f, 3f);
        float outsideSmall = ProximityMath.CalculateVolume("Farm", "Farm", 3f, 3f);
        float insideLarge = ProximityMath.CalculateVolume("Farm", "Farm", 6f, 10f);

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
