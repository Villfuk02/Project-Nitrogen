using System;
using System.Collections.Generic;

namespace Utils
{
    public struct Damage
    {
        [Flags] public enum Type { HealthLoss = 1 << 0, Physical = 1 << 1, Energy = 1 << 2, Explosive = 1 << 3 }

        public float amount;
        public Type type;
        public object source;

        public Damage(float amount, Type type, object source)
        {
            this.amount = amount;
            this.type = type;
            this.source = source;
        }

        public readonly override string ToString() => $"{amount} damage (type={type}, source={source})";

        public static string FormatDamage(int value, int original, Type type, TextUtils.Improvement improvement)
        {
            return $"{type.ToHumanReadable(true)}{TextUtils.FormatIntStat(null, value, original, improvement)}";
        }

        public static string FormatDamageType(Type type, Type original)
        {
            return $"{(type & original).ToHumanReadable(false)} {(type & ~original).ToHumanReadable(false).Colored(TextUtils.NEW_COLOR)}";
        }

        public static float CalculateDps(int damage, int intervalTicks) => intervalTicks == 0 ? 0 : damage * TimeUtils.TICKS_PER_SEC / (float)intervalTicks;
    }

    public static class DamageExtensions
    {
        static readonly string[] HumanReadableNames = { "Health Loss", "Physical", "Energy", "Explosive" };
        static readonly TextUtils.Icon[] Icons = { TextUtils.Icon.DmgHpLoss, TextUtils.Icon.DmgPhysical, TextUtils.Icon.DmgEnergy, TextUtils.Icon.DmgExplosive };

        public static string ToHumanReadable(this Damage.Type type, bool iconsOnly)
        {
            if (type == 0)
                return TextUtils.Icon.Damage.Sprite();

            List<string> types = new();
            int i = 0;
            while (type != 0)
            {
                if (((int)type & 1) != 0)
                    types.Add($"{Icons[i].Sprite()}{(iconsOnly ? "" : HumanReadableNames[i])}");
                type = (Damage.Type)((int)type >> 1);
                i++;
            }

            return string.Join(' ', types);
        }
    }
}