using System;
using System.Collections.Generic;

namespace Game.Damage
{
    public struct Damage
    {
        [Flags] public enum Type { HealthLoss = 1 << 0, Physical = 1 << 1, Fire = 1 << 2 }

        public float amount;
        public Type type;
        public IDamageSource source;

        public Damage(float amount, Type type, IDamageSource source)
        {
            this.amount = amount;
            this.type = type;
            this.source = source;
        }

        public override string ToString() => $"{amount} damage (type={type}, source={source})";
    }

    public static class DamageExtensions
    {
        static readonly string[] HumanReadableNames = { "Health Loss", "Physical", "Fire" };
        public static string ToHumanReadable(this Damage.Type type)
        {
            List<string> types = new();
            int i = 0;
            while (type != 0)
            {
                if (((int)type & 1) != 0)
                    types.Add(HumanReadableNames[i]);
                type = (Damage.Type)((int)type >> 1);
                i++;
            }
            return string.Join(' ', types);
        }
    }
}
