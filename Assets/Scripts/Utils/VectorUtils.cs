using System.Collections;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.Utils
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
    }
}
