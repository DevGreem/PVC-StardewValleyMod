using System;
using ProximityVoiceChat.Config;

namespace ProximityVoiceChat.Network;

/// <summary>Pure rules for accepting and resolving the host's wall-occlusion setting.</summary>
public static class WallOcclusionSync
{
    /// <summary>Get whether a settings message must be applied by this peer.</summary>
    public static bool ShouldApplyFromHost(
        bool isMainPlayer,
        long fromPlayerId,
        long hostPlayerId,
        string? fromModId,
        string? type)
    {
        return !isMainPlayer
            && hostPlayerId != 0L
            && fromPlayerId == hostPlayerId
            && string.Equals(fromModId, ModConstants.ModId, StringComparison.Ordinal)
            && string.Equals(type, ModConstants.WallOcclusionMessageType, StringComparison.Ordinal);
    }

    /// <summary>Resolve the value used by playback for the local player.</summary>
    public static bool ResolveEffective(bool isMainPlayer, bool hostPreference, bool hasHostValue, bool hostValue)
    {
        if (isMainPlayer)
            return hostPreference;

        return hasHostValue ? hostValue : true;
    }
}
