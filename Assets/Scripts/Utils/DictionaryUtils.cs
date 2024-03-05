using System.Collections.Generic;

namespace Utils
{
    public static class DictionaryUtils
    {
        public static void Increment<T>(this Dictionary<T, int> dict, T key, int value = 1)
        {
            if (!dict.TryAdd(key, value))
                dict[key] += value;
        }
    }
}
