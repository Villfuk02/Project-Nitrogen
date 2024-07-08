using UnityEngine;
using Utils;

namespace BattleSimulation.Attackers
{
    public class DamageResistance : MonoBehaviour
    {
        public Attacker attacker;
        public Damage.Type damageType;
        public float fractionReduction;
        public int flatReduction;

        static DamageResistance()
        {
            Attacker.DAMAGE.RegisterModifier(ReduceDamage, -10);
        }

        static bool ReduceDamage(ref (Attacker target, Damage damage) param)
        {
            param.target.TryGetComponent(out DamageResistance damageResistance);

            if (damageResistance == null || (param.damage.type & ~damageResistance.damageType) != 0)
                return true;
            param.damage.amount *= 1 - damageResistance.fractionReduction;
            param.damage.amount -= damageResistance.flatReduction;
            return param.damage.amount > 0;
        }
    }
}