using System;

namespace ProximityVoiceChat.Audio;

/// <summary>Pure gain and low-pass values for a number of static wall tiles.</summary>
public static class OcclusionMath
{
    public const int SilentBlockedTiles = 3;

    private const float GainPerBlockedTile = 0.4f;
    private const float AlphaPerBlockedTile = 0.45f;
    private const float AlphaFloor = 0.08f;

    /// <summary>Get the playback gain for a line that crosses the given number of solid tiles.</summary>
    public static float Gain(int blockedTiles)
    {
        if (blockedTiles <= 0)
            return 1f;

        return Math.Max(0f, 1f - (blockedTiles * GainPerBlockedTile));
    }

    /// <summary>Get the one-pole low-pass alpha for the given number of solid tiles.</summary>
    public static float LowPassAlpha(int blockedTiles)
    {
        if (blockedTiles <= 0)
            return 1f;

        return Math.Max(AlphaFloor, 1f - (blockedTiles * AlphaPerBlockedTile));
    }

    /// <summary>Get whether the occlusion is total.</summary>
    public static bool IsSilent(int blockedTiles)
    {
        return Gain(blockedTiles) <= 0f;
    }
}
