//Based on https://stackoverflow.com/a/11109361

using System;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.Utils
{
    public class ThreadSafeRandom
    {
        private static readonly Random _global = new();
        [ThreadStatic] private static Random _local;

        /// <summary>
        /// Returns random int between 0 (inclusive) and int.MaxValue (exclusive)
        /// </summary>
        public int Next()
        {
            Check();
            return _local.Next();
        }
        /// <summary>
        /// Returns random int between 0 (inclusive) and maxValue (exclusive)
        /// </summary>
        public int Next(int maxValue)
        {
            Check();
            return _local.Next(maxValue);
        }
        /// <summary>
        /// Returns random int between minValue (inclusive) and maxValue (exclusive)
        /// </summary>
        public int Next(int minValue, int maxValue)
        {
            Check();
            return _local.Next(minValue, maxValue);
        }
        /// <summary>
        /// Returns random float between 0 (inclusive) and 1 (inclusive)
        /// </summary>
        public float NextFloat()
        {
            Check();
            return (float)_local.NextDouble();
        }
        /// <summary>
        /// Returns random float between minValue (inclusive) and maxValue (inclusive)
        /// </summary>
        public float NextFloat(float minValue, float maxValue)
        {
            return minValue + (maxValue - minValue) * NextFloat();
        }

        void Check()
        {
            if (_local == null)
            {
                int seed;
                lock (_global)
                {
                    seed = _global.Next();
                }
                _local = new Random(seed);
            }
        }
    }
}
