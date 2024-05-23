using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class MathUtils
    {
        /// <summary>
        /// Returns the remainder of 'x' divided by 'm'. In particular, returns 'x' - 'd'*'m', where 'd' is the largest integer such that 'd'*'m' is less than or equal to 'x'.
        /// The divisor must be positive.
        /// </summary>
        /// <returns>The remainder in the range [0..m-1].</returns>
        public static int Mod(int x, int m) => (int)((x + ((long)m << 32)) % m);

        /// <summary>
        /// Rounds a number away from zero. The result is equal to ceil(x) for positive x and floor(x) for negative x.
        /// </summary>
        public static int RoundAwayFromZero(float x) => Math.Sign(x) * Mathf.CeilToInt(Mathf.Abs(x));

        /// <summary>
        /// Makes the smallest possible step from value towards target, such that it is closer to target by at least 1 / divisor.
        /// </summary>
        public static void StepTowards(ref int value, int target, int divisor) => value += RoundAwayFromZero((target - value) / (float)divisor);

        /// <summary>
        /// Constructs a bitmask with the lowest n bits set.
        /// n must be between 0 and 31
        /// </summary>
        public static uint LowestBitsSet(int bits) => (1u << bits) - 1;

        public static bool IsSet(this uint bitmask, int bit) => (bitmask & (1 << bit)) != 0;

        /// <summary>
        /// Enumerate the bits that are set.
        /// </summary>
        public static IEnumerable<int> GetBits(this uint bitmask)
        {
            int i = 0;
            while (bitmask != 0)
            {
                if ((bitmask & 1) != 0)
                    yield return i;
                bitmask >>= 1;
                i++;
            }
        }

        /// <summary>
        /// Counts the number of bits that are set.
        /// </summary>
        // Taken from https://stackoverflow.com/a/109025/7861895
        public static int PopCount(this uint bitmask)
        {
            bitmask -= (bitmask >> 1) & 0x55555555; // add pairs of bits
            bitmask = (bitmask & 0x33333333) + ((bitmask >> 2) & 0x33333333); // quads
            bitmask = (bitmask + (bitmask >> 4)) & 0x0F0F0F0F; // groups of 8
            bitmask *= 0x01010101; // horizontal sum of bytes
            return (int)(bitmask >> 24);
        }
    }
}