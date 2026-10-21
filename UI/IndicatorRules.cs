namespace ProximityVoiceChat.UI;

/// <summary>Pure decisions about what the on-screen indicator should show.</summary>
public static class IndicatorRules
{
    /// <summary>Get whether a nearby player should appear in the HUD list.</summary>
    public static bool IsNearbySpeakerVisible(bool isNearby, bool isSpeaking, bool isMuted)
    {
        return isNearby && (isSpeaking || isMuted);
    }

    /// <summary>Get whether the indicator should be drawn at all.</summary>
    public static bool ShouldDraw(bool showIndicator, bool worldReady, bool multiplayer)
    {
        return showIndicator && worldReady && multiplayer;
    }

    /// <summary>Format the local player's outgoing voice reach for the HUD.</summary>
    public static string FormatVoiceReach(int radius)
    {
        return $"Voice reach: {radius} tiles";
    }
}
