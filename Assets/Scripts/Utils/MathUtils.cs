namespace Utils
{
    public static class MathUtils
    {
        public static int Mod(int x, int m) => (int)((x + ((long)m << 32)) % m);
    }
}
