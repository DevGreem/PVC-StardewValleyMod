using System;
using System.Collections.Generic;
using ProximityVoiceChat.Audio;

namespace ProximityVoiceChat.Network;

/// <summary>A farmer snapshot used to decide who should receive an outgoing voice packet.</summary>
public readonly record struct VoiceRecipientCandidate(long PlayerId, string Location, float TileX, float TileY);

/// <summary>Pure recipient selection for outgoing voice packets.</summary>
public static class VoiceRecipientRules
{
    /// <summary>Select peers in the sender's location and inside the packet's declared reach.</summary>
    public static long[] Select(
        string? senderLocation,
        float senderTileX,
        float senderTileY,
        int radius,
        IEnumerable<VoiceRecipientCandidate> candidates)
    {
        if (string.IsNullOrEmpty(senderLocation) || radius <= 0)
            return Array.Empty<long>();

        List<long> recipients = new();
        foreach (VoiceRecipientCandidate candidate in candidates)
        {
            if (candidate.PlayerId <= 0
                || !ProximityMath.IsSameLocation(senderLocation, candidate.Location))
            {
                continue;
            }

            float deltaX = candidate.TileX - senderTileX;
            float deltaY = candidate.TileY - senderTileY;
            float distance = MathF.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            if (ProximityMath.IsInRange(distance, radius))
                recipients.Add(candidate.PlayerId);
        }

        return recipients.ToArray();
    }
}
