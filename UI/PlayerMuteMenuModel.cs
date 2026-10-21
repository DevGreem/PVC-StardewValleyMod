using System;
using System.Collections.Generic;

namespace ProximityVoiceChat.UI;

/// <summary>Pure filtering and ordering for the player-mute menu.</summary>
public static class PlayerMuteMenuModel
{
    public static IReadOnlyList<PlayerMuteRow> Build(
        IReadOnlyList<PlayerMuteCandidate> players,
        string localLocation,
        int localTileX,
        int localTileY,
        bool nearbyOnly,
        int reach)
    {
        List<PlayerMuteRow> rows = new();
        foreach (PlayerMuteCandidate player in players)
        {
            float distance = Distance(localTileX, localTileY, player.TileX, player.TileY);
            if (nearbyOnly)
            {
                if (!string.Equals(player.Location, localLocation, StringComparison.Ordinal))
                    continue;

                if (distance > reach)
                    continue;
            }

            rows.Add(new PlayerMuteRow(
                player.PlayerId,
                player.Name,
                player.Location,
                player.IsMuted,
                distance));
        }

        rows.Sort((first, second) =>
        {
            if (!nearbyOnly)
                return StringComparer.OrdinalIgnoreCase.Compare(first.Name, second.Name);

            int distanceOrder = first.Distance.CompareTo(second.Distance);
            return distanceOrder != 0
                ? distanceOrder
                : StringComparer.OrdinalIgnoreCase.Compare(first.Name, second.Name);
        });

        return rows;
    }

    private static float Distance(int fromX, int fromY, int toX, int toY)
    {
        int deltaX = toX - fromX;
        int deltaY = toY - fromY;
        return MathF.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }
}
