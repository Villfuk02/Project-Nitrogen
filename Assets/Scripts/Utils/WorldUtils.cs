using UnityEngine;

public static class WorldUtils
{
    public static readonly Vector2Int WORLD_SIZE = new(35, 19);
    public const int MAX_HEIGHT = 3;
    public const float HEIGHT_STEP = 0.5f;
    public static readonly Vector2Int[] CARDINAL_DIRS = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };
    public enum Slant { None, North, East, South, West };
    public enum TerrainType { White, Blue };
    public static Vector3 TileToWorldPos(Vector3Int tilePos)
    {
        return new Vector3(
            tilePos.x - (WORLD_SIZE.x - 1) / 2f,
            tilePos.y - (WORLD_SIZE.y - 1) / 2f,
            tilePos.z * HEIGHT_STEP
            );
    }
    public static Vector3 SlotToWorldPos(int x, int y)
    {
        return new Vector3(
            x - WORLD_SIZE.x / 2f,
            y - WORLD_SIZE.y / 2f,
            0
            );
    }

    public static Vector3Int WorldToTilePos(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(worldPos.x + (WORLD_SIZE.x - 1) / 2f),
            Mathf.RoundToInt(worldPos.y + (WORLD_SIZE.y - 1) / 2f),
            Mathf.RoundToInt(worldPos.z / HEIGHT_STEP)
            );
    }

    public static Vector2Int WorldToSlotPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x + WORLD_SIZE.x / 2f),
            Mathf.RoundToInt(worldPos.y + WORLD_SIZE.y / 2f)
            );
    }

    public static bool IsInRange(int x, int y, int sizeX, int sizeY)
    {
        return x >= 0 && y >= 0 && x < sizeX && y < sizeY;
    }

    public static Slant FlipSlant(Slant s)
    {
        return s switch
        {
            Slant.East => Slant.West,
            Slant.West => Slant.East,
            _ => s,
        };
    }
    public static Slant RotateSlant(Slant s, int r)
    {
        if (s == Slant.None || r == 0)
        {
            return s;
        }
        return (Slant)(((int)s + r - 1) % 4 + 1);
    }
}
