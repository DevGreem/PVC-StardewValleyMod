using System;
using ProximityVoiceChat.Config;

namespace ProximityVoiceChat.Network;

/// <summary>Validates semantic fields that structural parsing cannot judge on its own.</summary>
public static class PacketValidator
{
    /// <summary>Validate a parsed packet against the protocol and the offered radius set.</summary>
    public static bool IsValid(VoicePacket packet, out string error)
    {
        if (!packet.HasSupportedVersion())
        {
            error = $"unsupported wire-format version {packet.Version}";
            return false;
        }

        if (!ModConfig.IsOfferedRadius(packet.ProximityRadius)
            && !ModConfig.IsLegacyRadius(packet.ProximityRadius))
        {
            error = $"unsupported proximity radius {packet.ProximityRadius}";
            return false;
        }

        if (string.IsNullOrEmpty(packet.LocationName))
        {
            error = "location name is empty";
            return false;
        }

        if (packet.Audio.Length <= 0 || packet.Audio.Length > VoicePacket.MaxAudioBytes)
        {
            error = $"audio payload size {packet.Audio.Length} is outside the accepted range";
            return false;
        }

        error = "";
        return true;
    }
}
