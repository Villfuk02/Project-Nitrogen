
using System.Collections;
using UnityEngine;

namespace Utils
{
    public static class VectorUtils
    {
        public static IEnumerator GetEnumerator(this Vector2Int v)
        {
            if (v.x <= 0 || v.y <= 0)
                yield break;
            for (int x = 0; x < v.x; x++)
            {
                for (int y = 0; y < v.y; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }

        public static Vector2Int Round(this Vector2 v)
        {
            return new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        }
        public static Vector3Int Round(this Vector3 v)
        {
            return new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        }
    }
}
