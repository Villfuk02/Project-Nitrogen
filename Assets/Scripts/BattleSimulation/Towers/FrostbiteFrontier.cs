using BattleSimulation.Attackers;
using Game.Shared;
using UnityEngine;

namespace BattleSimulation.Towers
{
    public class FrostbiteFrontier : Tower
    {
        [Header("Settings")]
        [SerializeField] float speedReduction;
        [SerializeField] float damageIncrease;
        [SerializeField] Damage.Type damageType;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            Attacker.DAMAGE.RegisterModifier(DamageAttacker, -100);
            Attacker.SPEED.RegisterModifier(UpdateSpeed, -100);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
            {
                Attacker.DAMAGE.UnregisterModifier(DamageAttacker);
                Attacker.SPEED.UnregisterModifier(UpdateSpeed);
            }
        }

        void UpdateSpeed(Attacker attacker, ref float speed)
        {
            if (targeting.IsAmongTargets(attacker))
                speed *= 1 - speedReduction;
        }

        bool DamageAttacker(ref (Attacker target, Damage dmg) param)
        {
            if (param.dmg.type.HasFlag(damageType) && targeting.IsAmongTargets(param.target))
                param.dmg.amount *= 1 + damageIncrease;

            return true;
        }
    }
}