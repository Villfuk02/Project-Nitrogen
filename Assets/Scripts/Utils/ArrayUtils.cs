using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    public static class ArrayUtils
    {
        public static T[] PackFlat<T>(this ICollection<ICollection<T>> inputs, IList<int> lengths)
        {
            var array = new T[lengths.Sum()];
            int i = 0;
            int arrayIndex = 0;
            foreach (var collection in inputs)
            {
                collection.CopyTo(array, arrayIndex);
                arrayIndex += lengths[i];
                i++;
            }
            return array;
        }
        public static T[][] UnpackFlat<T>(this T[] array, IList<int> lengths)
        {
            int count = lengths.Count;
            var result = new T[count][];
            int arrayIndex = 0;
            for (int i = 0; i < count; i++)
            {
                result[i] = new T[lengths[i]];
                Array.Copy(array, arrayIndex, result[i], 0, lengths[i]);
                arrayIndex += lengths[i];
            }
            return result;
        }

        public static void AddMask(this Array2D<int> array, IReadOnlyArray2D<int> mask, Vector2Int position)
        {
            foreach ((Vector2Int offset, int value) in mask.IndexedEnumerable)
            {
                Vector2Int pos = position + offset;
                if (array.IsInBounds(pos))
                    array[pos] += value;
            }
        }
        public static void SubtractMask(this Array2D<int> array, IReadOnlyArray2D<int> mask, Vector2Int position)
        {
            foreach ((Vector2Int offset, int value) in mask.IndexedEnumerable)
            {
                Vector2Int pos = position + offset;
                if (array.IsInBounds(pos))
                    array[pos] -= value;
            }
        }
    }
}
