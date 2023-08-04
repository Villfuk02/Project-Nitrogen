namespace Utils
{
    public static class MathUtils
    {
        /// <summary>
        /// Returns the remainder of 'x' divided by 'm'. In particular, returns 'x' - 'd'*'m', where 'd' is the largest integer such that 'd'*'m' is less than or equal to 'x'. The divisor must be positive.
        /// </summary>
        /// <returns>The remainder in the range [0..m-1].</returns>
        public static int Mod(int x, int m) => (int)((x + ((long)m << 32)) % m);
    }
}
