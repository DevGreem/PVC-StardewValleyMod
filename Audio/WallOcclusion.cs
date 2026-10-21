using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using xTile.Layers;
using xTile.ObjectModel;

namespace ProximityVoiceChat.Audio;

/// <summary>Counts static wall tiles and constructed building footprints between two farmer tiles.</summary>
public static class WallOcclusion
{
    public const string LayerName = "Buildings";

    private const float SamplesPerTile = 2f;

    /// <summary>Count solid static tiles and building footprints between two tile positions.</summary>
    public static int CountBlockingTiles(GameLocation? location, Vector2 fromTile, Vector2 toTile)
    {
        if (location == null)
            return 0;

        Layer? layer = location.Map?.GetLayer(LayerName);
        return CountBlockingTiles(
            (int)Math.Round(fromTile.X),
            (int)Math.Round(fromTile.Y),
            (int)Math.Round(toTile.X),
            (int)Math.Round(toTile.Y),
            (x, y) => (layer != null && IsBlocking(layer, x, y)) || IsBuildingFootprint(location, x, y));
    }

    /// <summary>Count solid tiles between two tile positions using a caller-provided tile test.</summary>
    public static int CountBlockingTiles(int fromX, int fromY, int toX, int toY, Func<int, int, bool> isBlocking)
    {
        int deltaX = toX - fromX;
        int deltaY = toY - fromY;
        float distance = MathF.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        if (distance < 1f)
            return 0;

        int steps = (int)Math.Ceiling(distance * SamplesPerTile);
        if (steps < 2)
            return 0;

        int previousX = int.MinValue;
        int previousY = int.MinValue;
        int blocked = 0;

        for (int i = 1; i < steps; i++)
        {
            float progress = i / (float)steps;
            int x = (int)Math.Round(fromX + (deltaX * progress));
            int y = (int)Math.Round(fromY + (deltaY * progress));

            if (x == previousX && y == previousY)
                continue;

            previousX = x;
            previousY = y;

            if ((x == fromX && y == fromY) || (x == toX && y == toY))
                continue;

            if (isBlocking(x, y))
                blocked++;
        }

        return blocked;
    }

    /// <summary>Get whether a property value marks a tile as passable (for example, an open door).</summary>
    public static bool IsPassable(string? value)
    {
        return string.Equals(value, "T", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBlocking(Layer layer, int x, int y)
    {
        if (x < 0 || y < 0 || x >= layer.LayerWidth || y >= layer.LayerHeight)
            return false;

        var tile = layer.Tiles[x, y];
        if (tile == null)
            return false;

        if (TryGetPassable(tile.TileIndexProperties))
            return false;

        if (TryGetPassable(tile.Properties))
            return false;

        return true;
    }

    private static bool IsBuildingFootprint(GameLocation location, int x, int y)
    {
        if (location.buildings == null)
            return false;

        foreach (Building building in location.buildings)
        {
            if (building.occupiesTile(x, y, applyTilePropertyRadius: false))
                return true;
        }

        return false;
    }

    private static bool TryGetPassable(IPropertyCollection? properties)
    {
        if (properties == null || !properties.TryGetValue("Passable", out PropertyValue? value))
            return false;

        return IsPassable(value?.ToString());
    }
}
