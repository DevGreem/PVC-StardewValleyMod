using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using xTile.Layers;
using xTile.ObjectModel;

namespace ProximityVoiceChat.Audio;

/// <summary>Counts wall segments and constructed buildings between two farmer tiles.</summary>
public static class WallOcclusion
{
    public const string LayerName = "Buildings";

    private const float SamplesPerTile = 2f;
    private const int MapWallId = -1;

    /// <summary>Count wall segments and constructed buildings between two tile positions.</summary>
    public static int CountObstacles(GameLocation? location, Vector2 fromTile, Vector2 toTile)
    {
        if (location == null)
            return 0;

        Layer? layer = location.Map?.GetLayer(LayerName);
        return CountObstacles(
            (int)Math.Round(fromTile.X),
            (int)Math.Round(fromTile.Y),
            (int)Math.Round(toTile.X),
            (int)Math.Round(toTile.Y),
            (x, y) =>
            {
                Building? building = FindBuilding(location, x, y);
                if (building != null)
                    return GetBuildingId(building);

                if (layer != null && IsBlocking(layer, x, y))
                    return MapWallId;

                return 0;
            });
    }

    /// <summary>Count obstacles along a line using caller-provided obstacle ids.</summary>
    /// <remarks>
    /// 0 means clear, a negative value means a map wall run and a positive value means a
    /// building. Consecutive samples with the same id count as one obstacle.
    /// </remarks>
    public static int CountObstacles(int fromX, int fromY, int toX, int toY, Func<int, int, int> obstacleIdAt)
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
        int previousId = 0;
        int obstacles = 0;

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

            int id = obstacleIdAt(x, y);
            if (id == previousId)
                continue;

            if (id != 0)
                obstacles++;

            previousId = id;
        }

        return obstacles;
    }

    /// <summary>Get whether a property value marks a tile as passable (for example, an open door).</summary>
    public static bool IsPassable(string? value)
    {
        return string.Equals(value, "T", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static Building? FindBuilding(GameLocation location, int x, int y)
    {
        if (location.buildings == null)
            return null;

        foreach (Building building in location.buildings)
        {
            if (building.occupiesTile(x, y, applyTilePropertyRadius: false))
                return building;
        }

        return null;
    }

    private static int GetBuildingId(Building building)
    {
        int id = RuntimeHelpers.GetHashCode(building) & 0x7FFFFFFF;
        return id <= 1 ? 2 : id;
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

    private static bool TryGetPassable(IPropertyCollection? properties)
    {
        if (properties == null || !properties.TryGetValue("Passable", out PropertyValue? value))
            return false;

        return IsPassable(value?.ToString());
    }
}
