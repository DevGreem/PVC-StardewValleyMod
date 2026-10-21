using System;
using StardewValley;
using StardewValley.Locations;

namespace ProximityVoiceChat.Audio;

/// <summary>Location-based acoustic presets, separate from wall occlusion.</summary>
public static class EnvironmentAcoustics
{
    public const float CaveGain = 0.55f;
    public const float CaveLowPassAlpha = 0.35f;

    /// <summary>Get whether a location is a cave or mine.</summary>
    public static bool IsCave(GameLocation? location)
    {
        return location is MineShaft
            || location is FarmCave
            || location is IslandFarmCave;
    }

    /// <summary>Apply the cave gain to a playback gain.</summary>
    public static float ApplyGain(float gain, bool inCave)
    {
        return inCave ? gain * CaveGain : gain;
    }

    /// <summary>Apply the cave low-pass ceiling to an occlusion alpha.</summary>
    public static float ApplyLowPassAlpha(float alpha, bool inCave)
    {
        return inCave ? Math.Min(alpha, CaveLowPassAlpha) : alpha;
    }
}
