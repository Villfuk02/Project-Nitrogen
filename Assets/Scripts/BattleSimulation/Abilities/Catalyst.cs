using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Catalyst : Ability
    {
        [Header("References")]
        [SerializeField] Targeting.Targeting targeting;

        [Header("Settings")]
        [SerializeField] float damageIncrease;

        [Header("Runtime variables")]
        HashSet<Attacker> afflicted_;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.radius);
        }

        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.RegisterReaction(OnWaveFinished, 100);
            Attacker.DAMAGE.RegisterModifier(DamageAttacker, -100);
            Attacker.DIE.RegisterReaction(OnAttackerKilled, 100);

            afflicted_ = new(targeting.GetValidTargets());
        }

        protected void OnDestroy()
        {
            if (!Placed)
                return;

            WaveController.onWaveFinished.UnregisterReaction(OnWaveFinished);
            Attacker.DAMAGE.UnregisterModifier(DamageAttacker);
            Attacker.DIE.UnregisterReaction(OnAttackerKilled);
        }

        bool DamageAttacker(ref (Attacker target, Damage dmg) param)
        {
            if (afflicted_.Contains(param.target))
                param.dmg.amount *= 1 + damageIncrease;

            return true;
        }

        void OnAttackerKilled((Attacker target, Damage cause) param)
        {
            if (afflicted_.Contains(param.target))
                BattleController.addMaterial.Invoke((param.target, Blueprint.materialProduction));
        }


        void OnWaveFinished()
        {
            Destroy(gameObject);
        }
    }
}