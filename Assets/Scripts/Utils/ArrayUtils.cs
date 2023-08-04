using UnityEngine;

namespace Utils
{
    public static class ArrayUtils
    {
        public static void Add(this Array2D<int> array, IReadOnlyArray2D<int> addend, Vector2Int position)
        {
            foreach ((Vector2Int offset, int value) in addend.IndexedEnumerable)
            {
                Vector2Int pos = position + offset;
                if (array.IsInBounds(pos))
                    array[pos] += value;
            }
        }
        public static void Subtract(this Array2D<int> array, IReadOnlyArray2D<int> subtrahend, Vector2Int position)
        {
            foreach ((Vector2Int offset, int value) in subtrahend.IndexedEnumerable)
            {
                Vector2Int pos = position + offset;
                if (array.IsInBounds(pos))
                    array[pos] -= value;
            }
        }
    }
}
