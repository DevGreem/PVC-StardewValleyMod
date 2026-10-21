using System;

namespace ProximityVoiceChat.Audio;

/// <summary>Pure proximity math shared by playback and its tests.</summary>
public static class ProximityMath
{
    /// <summary>
    /// Get the playback gain for a speaker: full volume at zero distance,
    /// silence at or beyond the radius, and a linear falloff in between.
    /// </summary>
    public static float Volume(float distanceTiles, float radiusTiles)
    {
        if (radiusTiles <= 0f || distanceTiles >= radiusTiles)
            return 0f;

        if (distanceTiles <= 0f)
            return 1f;

        return 1f - (distanceTiles / radiusTiles);
    }

    /// <summary>Get whether a speaker is inside the configured radius.</summary>
    public static bool IsInRange(float distanceTiles, float radiusTiles)
    {
        return radiusTiles > 0f && distanceTiles < radiusTiles;
    }

    /// <summary>
    /// Get the playback gain for a speaker after applying the same-location gate
    /// and the distance falloff. The master volume is applied by the mixer.
    /// </summary>
    public static float CalculateVolume(string? localLocation, string? speakerLocation, float distanceTiles, float radiusTiles)
    {
        if (!IsSameLocation(localLocation, speakerLocation))
            return 0f;

        return Volume(distanceTiles, radiusTiles);
    }

    /// <summary>Compare two game location identifiers for the same-location gate.</summary>
    public static bool IsSameLocation(string? first, string? second)
    {
        return !string.IsNullOrEmpty(first)
            && string.Equals(first, second, StringComparison.Ordinal);
    }
}
