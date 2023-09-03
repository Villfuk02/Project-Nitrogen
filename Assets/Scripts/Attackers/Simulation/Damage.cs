using System;

namespace Attackers.Simulation
{
    public struct Damage
    {
        [Flags] public enum Type { HealthLoss = 1 << 0, Physical = 1 << 1, Fire = 1 << 2 }

        public int amount;
        public Type type;

        public Damage(int amount, Type type)
        {
            this.amount = amount;
            this.type = type;
        }
    }
}
