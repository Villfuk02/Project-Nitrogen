using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Utils
{
    public static class TextUtils
    {
        public static readonly string IMPROVED_COLOR = "#" + ColorUtility.ToHtmlStringRGBA(Color.green);
        public static readonly string WORSENED_COLOR = "#" + ColorUtility.ToHtmlStringRGBA(0.8f * Color.red + 0.2f * Color.white);
        public static readonly string CHANGED_COLOR = "#" + ColorUtility.ToHtmlStringRGBA(0.7f * Color.yellow + 0.3f * Color.white);
        public static readonly string NEW_COLOR = "#" + ColorUtility.ToHtmlStringRGBA(0.7f * Color.yellow + 0.3f * Color.red);

        public enum Improvement { More, Less, Undeclared }

        public enum Icon
        {
            Materials, Fuel, Energy, Damage, Dps, Fire, DmgHpLoss, Hull, DmgPhysical, Range,
            Interval, Production, Boss, Health, Large, Small, Speed, DmgExplosive, Radius, Duration,
            Delay, DmgEnergy
        }

        public static string Sprite(this Icon icon) => $"<sprite={(int)icon}>";
        public static string Colored(this string text, string color) => $"<color={color}>{text}</color>";

        public static string FormatStringStat(string text, string? original)
        {
            if (text == original)
                return text;
            return text.Colored(string.IsNullOrEmpty(original) ? NEW_COLOR : CHANGED_COLOR);
        }

        public static string FormatIntStat(Icon? icon, int value, int original, Improvement improvement)
        {
            return ColorImprovement($"{icon?.Sprite()}{value}", value, original, improvement);
        }

        public static string FormatFloatStat(Icon? icon, float value, float original, Improvement improvement)
        {
            return ColorImprovement($"{icon?.Sprite()}{value.ToString("0.##", CultureInfo.InvariantCulture)}", value, original, improvement);
        }

        public static string FormatTicksStat(Icon? icon, int ticks, int original, Improvement improvement)
        {
            return ColorImprovement($"{icon?.Sprite()}{(ticks * TimeUtils.SECS_PER_TICK).ToString("0.##", CultureInfo.InvariantCulture)}s", ticks, original, improvement);
        }

        public static string FormatProduction(int fuel, int materials, int energy, int originalFuel, int originalMaterials, int originalEnergy)
        {
            StringBuilder sb = new();
            sb.Append(Icon.Production.Sprite());
            if (fuel > 0)
                sb.Append(FormatIntStat(Icon.Fuel, fuel, originalFuel, Improvement.More));
            if (materials > 0)
                sb.Append(FormatIntStat(Icon.Materials, materials, originalMaterials, Improvement.More));
            if (energy > 0)
                sb.Append(FormatIntStat(Icon.Energy, energy, originalEnergy, Improvement.More));
            return sb.ToString();
        }

        public static string FormatDuration(int ticks, int originalTicks, int waves, int originalWaves)
        {
            if (ticks > 0)
                return FormatTicksStat(Icon.Duration, ticks, originalTicks, Improvement.More);
            return $"{FormatIntStat(Icon.Duration, waves, originalWaves, Improvement.More)} waves";
        }

        public static string FormatCooldownSuffix(int cooldown, int originalCooldown)
        {
            if (cooldown > 0)
                return $"/{FormatIntStat(null, cooldown, originalCooldown, Improvement.Less)} waves";
            return " waves";
        }

        public static string ColorImprovement<T>(string text, T stat, T original, Improvement improvement) where T : IComparable<T>
        {
            int comparison = stat.CompareTo(original);
            if (comparison == 0)
                return text;
            if (improvement == Improvement.Undeclared)
                return text.Colored(CHANGED_COLOR);

            if (improvement == Improvement.Less)
                comparison *= -1;

            return text.Colored(comparison > 0 ? IMPROVED_COLOR : WORSENED_COLOR);
        }
    }
}