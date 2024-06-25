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

        void Awake()
        {
            Attacker.DAMAGE.RegisterModifier(ReduceDamage, -10);
        }

        void OnDestroy()
        {
            Attacker.DAMAGE.UnregisterModifier(ReduceDamage);
        }


        bool ReduceDamage(ref (Attacker target, Damage damage) param)
        {
            if (param.target != attacker || (param.damage.type & ~damageType) != 0)
                return true;
            param.damage.amount *= 1 - fractionReduction;
            param.damage.amount -= flatReduction;
            return param.damage.amount > 0;
        }
    }
}