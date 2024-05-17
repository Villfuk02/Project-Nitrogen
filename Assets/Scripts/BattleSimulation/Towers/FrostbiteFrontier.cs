using BattleSimulation.Attackers;
using Game.Damage;
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
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                Attacker.DAMAGE.UnregisterModifier(DamageAttacker);
        }

        public void OnAttackerEnter(Attacker attacker)
        {
            attacker.stats.speed *= 1 - speedReduction;
        }

        public void OnAttackerLeave(Attacker attacker)
        {
            attacker.stats.speed /= 1 - speedReduction;
        }

        bool DamageAttacker(ref (Attacker target, Damage dmg) param)
        {
            if (param.dmg.type.HasFlag(damageType) && targeting.IsAmongTargets(param.target))
                param.dmg.amount *= 1 + damageIncrease;

            return true;
        }
    }
}