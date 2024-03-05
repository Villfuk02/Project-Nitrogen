using System;
using System.Collections.Generic;
using Utils;

namespace Game.Damage
{
    public struct Damage
    {
        [Flags] public enum Type { HealthLoss = 1 << 0, Physical = 1 << 1, Fire = 1 << 2, Explosive = 1 << 3, Electric = 1 << 4 }

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
    }

    public static class DamageExtensions
    {
        static readonly string[] HumanReadableNames = { "Health Loss", "Physical", "Fire", "Explosive", "Electric" };
        static readonly TextUtils.Icon[] Icons = { TextUtils.Icon.HpLoss, TextUtils.Icon.Physical, TextUtils.Icon.Fire, TextUtils.Icon.Explosive, TextUtils.Icon.Energy };
        public static string ToHumanReadable(this Damage.Type type, bool icons)
        {
            List<string> types = new();
            int i = 0;
            while (type != 0)
            {
                if (((int)type & 1) != 0)
                    types.Add($"{(icons ? Icons[i].Sprite() : "")}{HumanReadableNames[i]}");
                type = (Damage.Type)((int)type >> 1);
                i++;
            }
            return string.Join(' ', types);
        }
    }
}
