using System;

namespace Game.Damage
{
    public struct Damage
    {
        [Flags] public enum Type { HealthLoss = 1 << 0, Physical = 1 << 1, Fire = 1 << 2 }

        public int amount;
        public Type type;
        public IDamageSource source;

        public Damage(int amount, Type type, IDamageSource source)
        {
            this.amount = amount;
            this.type = type;
            this.source = source;
        }
    }
}
