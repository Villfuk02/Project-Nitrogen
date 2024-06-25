using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class VectorUtils
    {
        /// <summary>
        /// Enumerates all <see cref="Vector2Int"/>s with x in the range [0..bounds.x-1] and y in the range [0..bounds.y-1]
        /// </summary>
        public static IEnumerator<Vector2Int> GetEnumerator(this Vector2Int bounds)
        {
            for (int x = 0; x < bounds.x; x++)
            for (int y = 0; y < bounds.y; y++)
                yield return new(x, y);
        }

        public static Vector2Int Round(this Vector2 v) => new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        public static Vector3Int Round(this Vector3 v) => new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        public static int ManhattanMagnitude(this Vector2Int v) => Mathf.Abs(v.x) + Mathf.Abs(v.y);
        public static float ManhattanMagnitude(this Vector2 v) => Mathf.Abs(v.x) + Mathf.Abs(v.y);
        public static int ManhattanMagnitude(this Vector3Int v) => Mathf.Abs(v.x) + Mathf.Abs(v.y) + Mathf.Abs(v.z);
        public static float ManhattanMagnitude(this Vector3 v) => Mathf.Abs(v.x) + Mathf.Abs(v.y) + Mathf.Abs(v.z);
        public static int ManhattanDistance(this Vector2Int a, Vector2Int b) => ManhattanMagnitude(a - b);
        public static float ManhattanDistance(this Vector2 a, Vector2 b) => ManhattanMagnitude(a - b);
        public static int ManhattanDistance(this Vector3Int a, Vector3Int b) => ManhattanMagnitude(a - b);
        public static float ManhattanDistance(this Vector3 a, Vector3 b) => ManhattanMagnitude(a - b);
        public static Vector2 XZ(this Vector3 v) => new(v.x, v.z);

        /// <summary>
        /// Is v.x in the range [0..size.x-1] and v.y in the range [0..size.y-1]?
        /// </summary>
        public static bool IsInRange(Vector2Int v, Vector2Int size) => v is { x: >= 0, y: >= 0 } && v.x < size.x && v.y < size.y;

        /// <summary>
        /// Is v.x in the range [0..size.x] and v.y in the range [0..size.y]?
        /// </summary>
        public static bool IsInRange(Vector2 v, Vector2 size) => v is { x: >= 0, y: >= 0 } && v.x <= size.x && v.y <= size.y;
    }
}