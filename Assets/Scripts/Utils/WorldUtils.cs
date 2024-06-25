using UnityEngine;

namespace Utils
{
    public static class WorldUtils
    {
        public static readonly Vector2Int WORLD_SIZE = new(15, 15);
        public static readonly float HEIGHT_STEP = 0.5f;
        public static readonly float SLANT_ANGLE = Mathf.Atan(HEIGHT_STEP) * Mathf.Rad2Deg;
        public static readonly Vector2Int WORLD_CENTER = (WORLD_SIZE - Vector2Int.one) / 2;
        public static readonly CardinalDirs<Vector2Int> CARDINAL_DIRS = new(Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left);
        public static readonly Vector2Int[] ADJACENT_DIRS = { Vector2Int.up, Vector2Int.one, Vector2Int.right, new(1, -1), Vector2Int.down, new(-1, -1), Vector2Int.left, new(-1, 1) };
        public static readonly DiagonalDirs<Vector2Int> DIAGONAL_DIRS = new(Vector2Int.one, new(1, -1), new(-1, -1), new(-1, 1));
        public static readonly Vector2Int[] ADJACENT_AND_ZERO = { Vector2Int.zero, Vector2Int.up, Vector2Int.one, Vector2Int.right, new(1, -1), Vector2Int.down, new(-1, -1), Vector2Int.left, new(-1, 1) };
        public static readonly CardinalDirs<Vector3> WORLD_CARDINAL_DIRS = new(Vector3.forward, Vector3.right, Vector3.back, Vector3.left);

        public enum Slant { None, North, East, South, West }

        public static readonly BitSet32 ALL_SLANTS = BitSet32.LowestBitsSet(5);

        public static Vector3 TilePosToWorldPos(Vector3 tilePos) => TilePosToWorldPos(tilePos.x, tilePos.y, tilePos.z);
        public static Vector3 TilePosToWorldPos(Vector2 tilePos, float height) => TilePosToWorldPos(tilePos.x, tilePos.y, height);
        public static Vector3 TilePosToWorldPos(Vector2Int tilePos) => TilePosToWorldPos((Vector2)tilePos);

        public static Vector3 TilePosToWorldPos(float x, float y, float height)
        {
            return new(
                x - (WORLD_SIZE.x - 1) / 2f,
                height * HEIGHT_STEP,
                y - (WORLD_SIZE.y - 1) / 2f
            );
        }

        public static Vector3 SlotPosToWorldPos(int x, int y) => SlotPosToWorldPos(x, y, 0);

        public static Vector3 SlotPosToWorldPos(float x, float y, float height)
        {
            return new(
                x - WORLD_SIZE.x / 2f,
                height * HEIGHT_STEP,
                y - WORLD_SIZE.y / 2f
            );
        }

        public static Vector3 WorldPosToTilePos(Vector3 worldPos)
        {
            return new(
                worldPos.x + (WORLD_SIZE.x - 1) / 2f,
                worldPos.z + (WORLD_SIZE.y - 1) / 2f,
                worldPos.y / HEIGHT_STEP
            );
        }

        public static Vector2 WorldPosToSlotPos(Vector3 worldPos)
        {
            return new(
                worldPos.x + WORLD_SIZE.x / 2f,
                worldPos.z + WORLD_SIZE.y / 2f
            );
        }

        /// <summary>
        /// Flips the slant, switching the east and west directions.
        /// </summary>
        public static Slant FlipSlant(Slant s)
        {
            return s switch
            {
                Slant.East => Slant.West,
                Slant.West => Slant.East,
                _ => s
            };
        }

        /// <summary>
        /// Rotates the slant by 'r' 90 degree rotations clockwise.
        /// </summary>
        public static Slant RotateSlant(Slant s, int r)
        {
            if (s == Slant.None || r == 0)
            {
                return s;
            }

            return (Slant)(MathUtils.Mod((int)s + r - 1, 4) + 1);
        }
    }
}