using System;
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
    }
}