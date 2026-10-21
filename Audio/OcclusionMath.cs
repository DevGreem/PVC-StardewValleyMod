using System;

namespace ProximityVoiceChat.Audio;

/// <summary>Pure gain and low-pass values for a number of static obstacles.</summary>
public static class OcclusionMath
{
    public const int SilentObstacles = 3;

    private const float GainPerObstacle = 0.4f;
    private const float AlphaPerObstacle = 0.45f;
    private const float AlphaFloor = 0.08f;

    /// <summary>Get the playback gain for a line that crosses the given number of obstacles.</summary>
    public static float Gain(int obstacles)
    {
        if (obstacles <= 0)
            return 1f;

        return Math.Max(0f, 1f - (obstacles * GainPerObstacle));
    }

    /// <summary>Get the one-pole low-pass alpha for the given number of obstacles.</summary>
    public static float LowPassAlpha(int obstacles)
    {
        if (obstacles <= 0)
            return 1f;

        return Math.Max(AlphaFloor, 1f - (obstacles * AlphaPerObstacle));
    }

    /// <summary>Get whether the occlusion is total.</summary>
    public static bool IsSilent(int obstacles)
    {
        return Gain(obstacles) <= 0f;
    }
}
