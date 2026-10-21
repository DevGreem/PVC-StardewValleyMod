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
}
