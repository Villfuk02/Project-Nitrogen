namespace Utils
{
    public static class TextUtils
    {
        public enum Icon { Materials, Fuel, Energy }

        public static string Sprite(this Icon icon) => $"<sprite={(int)icon}>";
    }
}
