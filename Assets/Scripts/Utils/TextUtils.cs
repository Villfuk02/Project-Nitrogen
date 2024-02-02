namespace Utils
{
    public static class TextUtils
    {
        public enum Icon { Materials, Fuel, Energy, Damage, Dps, Fire, HpLoss, Hull, Physical, Range, ShotInterval, Production, Boss, Health, Large, Small, Speed }

        public static string Sprite(this Icon icon) => $"<sprite={(int)icon}>";
        public static string Colored(this string text, string color) => $"<color={color}>{text}</color>";
    }
}
