using System;

namespace ProximityVoiceChat.UI;

/// <summary>Pure decisions about what the on-screen indicator should show.</summary>
public static class IndicatorRules
{
    /// <summary>Maximum number of player rows shown before the overflow summary.</summary>
    public const int MaxVisibleNearbyPlayers = 7;

    /// <summary>Get whether a nearby player should appear in the HUD list.</summary>
    public static bool IsNearbySpeakerVisible(bool isNearby, bool isSpeaking, bool isMuted)
    {
        return isNearby && (isSpeaking || isMuted);
    }

    /// <summary>Get how many player rows fit before the overflow summary.</summary>
    public static int GetVisibleCount(int totalNearbyPlayers)
    {
        return Math.Clamp(totalNearbyPlayers, 0, MaxVisibleNearbyPlayers);
    }

    /// <summary>Get how many nearby players are represented by the overflow summary.</summary>
    public static int GetHiddenCount(int totalNearbyPlayers)
    {
        return Math.Max(0, totalNearbyPlayers - MaxVisibleNearbyPlayers);
    }

    /// <summary>Format the additional-player summary row, or an empty string when none are hidden.</summary>
    public static string FormatNearbyOverflow(int hiddenCount)
    {
        if (hiddenCount <= 0)
            return "";

        return hiddenCount == 1
            ? "+1 player nearby"
            : $"+{hiddenCount} players nearby";
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
