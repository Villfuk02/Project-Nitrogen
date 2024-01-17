namespace Utils
{
    public static class TextUtils
    {
        public enum Icon { Materials, Fuel, Energy, Damage, Dps, Fire, HpLoss, Hull, Physical, Range, ShotInterval, Generation }

        public static string Sprite(this Icon icon) => $"<sprite={(int)icon}>";
        public static string Colored(this string text, string color) => $"<color={color}>{text}</color>";
    }
}
