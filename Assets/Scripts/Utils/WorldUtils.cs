
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class WorldUtils
    {
        public static readonly Vector2Int WORLD_SIZE = new(15, 15);
        public const float HEIGHT_STEP = 0.5f;
        public static readonly float SLANT_ANGLE = Mathf.Atan(HEIGHT_STEP) * Mathf.Rad2Deg;
        public static readonly Vector2Int ORIGIN = (WORLD_SIZE - Vector2Int.one) / 2;
        public static readonly CardinalDirs<Vector2Int> CARDINAL_DIRS = new(Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left);
        public static readonly Vector2Int[] ADJACENT_DIRS = { Vector2Int.up, Vector2Int.one, Vector2Int.right,
            new(1, -1), Vector2Int.down, new(-1, -1), Vector2Int.left, new(-1, 1) };
        public static readonly DiagonalDirs<Vector2Int> DIAGONAL_DIRS = new(Vector2Int.one, new(1, -1), new(-1, -1), new(-1, 1));
        public static readonly Vector2Int[] ADJACENT_AND_ZERO = { Vector2Int.zero, Vector2Int.up, Vector2Int.one, Vector2Int.right,
            new(1, -1), Vector2Int.down, new(-1, -1), Vector2Int.left, new(-1, 1) };
        public static readonly CardinalDirs<Vector3> WORLD_CARDINAL_DIRS = new(Vector3.forward, Vector3.right, Vector3.back, Vector3.left);
        public enum Slant { None, North, East, South, West };
        public static readonly Slant[] ALL_SLANTS = (Slant[])Enum.GetValues(typeof(Slant));

        public static Vector3 TileToWorldPos(Vector3 tilePos)
        {
            return TileToWorldPos(tilePos.x, tilePos.y, tilePos.z);
        }
        public static Vector3 TileToWorldPos(Vector2 tilePos, float height)
        {
            return TileToWorldPos(tilePos.x, tilePos.y, height);
        }
        public static Vector3 TileToWorldPos(Vector2Int tilePos)
        {
            return TileToWorldPos((Vector2)tilePos);
        }
        public static Vector3 TileToWorldPos(float x, float y, float height)
        {
            return new(
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
            return new(
                x - WORLD_SIZE.x / 2f,
                height * HEIGHT_STEP,
                y - WORLD_SIZE.y / 2f
                );
        }

        public static Vector3 WorldToTilePos(Vector3 worldPos)
        {
            return new(
                worldPos.x + (WORLD_SIZE.x - 1) / 2f,
                worldPos.z + (WORLD_SIZE.y - 1) / 2f,
                worldPos.y / HEIGHT_STEP
                );
        }

        public static Vector2 WorldToSlotPos(Vector3 worldPos)
        {
            return new(
                worldPos.x + WORLD_SIZE.x / 2f,
                worldPos.z + WORLD_SIZE.y / 2f
                );
        }

        public static Vector2Int GetMainDir(Vector2Int origin, Vector2Int tilePos, Random.Random random)
        {
            Vector2Int o = tilePos - origin;
            if (Mathf.Abs(o.x) == Mathf.Abs(o.y))
            {
                o += random.Float() < 0.5f ? Vector2Int.right : Vector2Int.left;
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

        public static bool IsInRange(Vector2Int v, Vector2Int size) => v is { x: >= 0, y: >= 0 } && v.x < size.x && v.y < size.y;

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
    public struct CardinalDirs<T> : IEnumerable<T>
    {
        public T N;
        public T E;
        public T S;
        public T W;

        public CardinalDirs(T n, T e, T s, T w)
        {
            N = n;
            E = e;
            S = s;
            W = w;
        }

        public T this[int index]
        {
            readonly get => MathUtils.Mod(index, 4) switch
            {
                0 => N,
                1 => E,
                2 => S,
                3 => W,
                _ => throw new InvalidOperationException()
            };
            set
            {
                switch (MathUtils.Mod(index, 4))
                {
                    case 0: N = value; break;
                    case 1: E = value; break;
                    case 2: S = value; break;
                    case 3: W = value; break;
                }
            }
        }

        public readonly CardinalDirs<TR> Map<TR>(Func<T, TR> map) => new(map(N), map(E), map(S), map(W));
        public readonly CardinalDirs<T> Rotated(int steps) =>
            new(this[-steps], this[1 - steps], this[2 - steps], this[3 - steps]);
        public readonly CardinalDirs<T> Rotated(int steps, Func<T, int, T> rotate) =>
            new(rotate(this[-steps], steps), rotate(this[1 - steps], steps),
                rotate(this[2 - steps], steps), rotate(this[3 - steps], steps));
        public readonly CardinalDirs<T> Flipped() => new(N, W, S, E);
        public readonly CardinalDirs<T> Flipped(Func<T, T> flip) => new(flip(N), flip(W), flip(S), flip(E));
        public readonly IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < 4; i++)
                yield return this[i];
        }
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct DiagonalDirs<T> : IEnumerable<T>
    {
        public T NW;
        public T NE;
        public T SE;
        public T SW;

        public DiagonalDirs(T nw, T ne, T se, T sw)
        {
            NW = nw;
            NE = ne;
            SE = se;
            SW = sw;
        }

        public T this[int index]
        {
            readonly get => MathUtils.Mod(index, 4) switch
            {
                0 => NW,
                1 => NE,
                2 => SE,
                3 => SW,
                _ => throw new InvalidOperationException()
            };
            set
            {
                switch (MathUtils.Mod(index, 4))
                {
                    case 0: NW = value; break;
                    case 1: NE = value; break;
                    case 2: SE = value; break;
                    case 3: SW = value; break;
                }
            }
        }

        public readonly DiagonalDirs<TR> Map<TR>(Func<T, TR> map) => new(map(NW), map(NE), map(SE), map(SW));
        public readonly DiagonalDirs<T> Rotated(int steps) =>
            new(this[-steps], this[1 - steps], this[2 - steps], this[3 - steps]);
        public readonly DiagonalDirs<T> Rotated(int steps, Func<T, int, T> rotate) =>
            new(rotate(this[-steps], steps), rotate(this[1 - steps], steps),
                rotate(this[2 - steps], steps), rotate(this[3 - steps], steps));
        public readonly DiagonalDirs<T> Flipped() => new(NE, NW, SW, SE);
        public readonly DiagonalDirs<T> Flipped(Func<T, T> flip) => new(flip(NE), flip(NW), flip(SW), flip(SE));
        public readonly IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < 4; i++)
                yield return this[i];
        }
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
