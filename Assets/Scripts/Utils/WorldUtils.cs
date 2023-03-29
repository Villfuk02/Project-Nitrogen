using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class WorldUtils
    {
        public static readonly Vector2Int WORLD_SIZE = new(15, 15);
        public const int MAX_HEIGHT = 3;
        public static readonly int[] ALL_HEIGHTS = Enumerable.Range(0, MAX_HEIGHT + 1).ToArray();
        public const float HEIGHT_STEP = 0.5f;
        public static readonly float SLANT_ANGLE = Mathf.Atan(HEIGHT_STEP) * Mathf.Rad2Deg;
        public static readonly Vector2Int ORIGIN = (WORLD_SIZE - Vector2Int.one) / 2;
        public static readonly Vector2Int[] CARDINAL_DIRS = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        public static readonly Vector2Int[] ADJACENT_DIRS = new Vector2Int[] { Vector2Int.up, Vector2Int.one, Vector2Int.right, new(1, -1), Vector2Int.down, new(-1, -1), Vector2Int.left, new(-1, 1) };
        public static readonly Vector2Int[] ADJACENT_AND_ZERO = new Vector2Int[] { Vector2Int.zero, Vector2Int.up, Vector2Int.one, Vector2Int.right, new(1, -1), Vector2Int.down, new(-1, -1), Vector2Int.left, new(-1, 1) };
        public static readonly Vector3[] WORLD_CARDINAL_DIRS = new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        public enum Slant { None, North, East, South, West };
        public static readonly Slant[] ALL_SLANTS = (Slant[])System.Enum.GetValues(typeof(Slant));
        public enum TerrainType { White, Blue };
        public static readonly TerrainType[] ALL_TERRAIN_TYPES = (TerrainType[])System.Enum.GetValues(typeof(TerrainType));
        static readonly ThreadSafeRandom _random = new();


        public static Vector3 TileToWorldPos(Vector3 tilePos)
        {
            return TileToWorldPos(tilePos.x, tilePos.y, tilePos.z);
        }
        public static Vector3 TileToWorldPos(Vector2Int tilePos)
        {
            return TileToWorldPos((Vector2)tilePos);
        }
        public static Vector3 TileToWorldPos(float x, float y, float height)
        {
            return new Vector3(
                x - (WORLD_SIZE.x - 1) / 2f,
                height * HEIGHT_STEP,
                y - (WORLD_SIZE.y - 1) / 2f
                );
        }

        public static Vector3 SlotToWorldPos(int x, int y)
        {
            return SlotToWorldPos(x, y, 0);
        }
        public static Vector3 SlotToWorldPos(float x, float y, float height)
        {
            return new Vector3(
                x - WORLD_SIZE.x / 2f,
                height * HEIGHT_STEP,
                y - WORLD_SIZE.y / 2f
                );
        }

        public static Vector3Int WorldToTilePos(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x + (WORLD_SIZE.x - 1) / 2f),
                Mathf.RoundToInt(worldPos.z / HEIGHT_STEP),
                Mathf.RoundToInt(worldPos.y + (WORLD_SIZE.y - 1) / 2f)
                );
        }

        public static Vector2Int WorldToSlotPos(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x + WORLD_SIZE.x / 2f),
                Mathf.RoundToInt(worldPos.z + WORLD_SIZE.y / 2f)
                );
        }

        public static Vector2Int GetMainDir(Vector2Int origin, Vector2Int tilePos)
        {
            Vector2Int o = tilePos - origin;
            if (Mathf.Abs(o.x) == Mathf.Abs(o.y))
            {
                o += _random.NextFloat() < 0.5f ? Vector2Int.right : Vector2Int.left;
            }
            if (Mathf.Abs(o.x) > Mathf.Abs(o.y))
            {
                return new((int)Mathf.Sign(o.x), 0);
            }
            else
            {
                return new(0, (int)Mathf.Sign(o.y));
            }
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
}
