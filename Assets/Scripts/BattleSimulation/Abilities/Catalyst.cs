using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Shared;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Catalyst : TargetedAbility
    {
        [Header("Settings")]
        [SerializeField] float damageIncrease;

        [Header("Runtime variables")]
        HashSet<Attacker> afflicted_;

        protected override void OnPlaced()
        {
            WaveController.ON_WAVE_FINISHED.RegisterReaction(OnWaveFinished, 100);
            Attacker.DAMAGE.RegisterModifier(DamageAttacker, -100);
            Attacker.DIE.RegisterReaction(OnAttackerKilled, 100);

            afflicted_ = new(targeting.GetValidTargets());

            SoundController.PlaySound(SoundController.Sound.Catalyst, 1, 1, 0.2f, transform.position);
        }

        protected void OnDestroy()
        {
            if (!Placed)
                return;

            WaveController.ON_WAVE_FINISHED.UnregisterReaction(OnWaveFinished);
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
                BattleController.ADD_MATERIAL.Invoke((param.target, currentBlueprint.materialProduction));
        }

        void OnWaveFinished()
        {
            Destroy(gameObject);
        }
    }
}