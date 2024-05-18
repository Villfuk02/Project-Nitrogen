using System.Linq;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Abilities
{
    public class LightningStorm : TargetedAbility
    {
        [Header("Settings")]
        [SerializeField] UnityEvent<Transform> onHit;
        public int strikes;

        [Header("Runtime variables")]
        int timer_;

        protected override void OnPlaced()
        {
            WaveController.ON_WAVE_FINISHED.RegisterReaction(OnWaveFinished, 100);
        }

        protected void OnDestroy()
        {
            if (Placed)
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(OnWaveFinished);
        }

        protected override void FixedUpdateInternal()
        {
            base.FixedUpdateInternal();
            if (!Placed || strikes < 0)
                return;
            if (strikes == 0)
            {
                strikes--;
                Destroy(gameObject, 5f);
                return;
            }

            if (timer_ == 0)
                Strike();
            timer_--;
        }

        void Strike()
        {
            strikes--;
            timer_ = Blueprint.interval;
            var targets = targeting.GetValidTargets().ToArray();
            if (targets.Length <= 0)
                return;
            Hit(targets[Random.Range(0, targets.Length)]);
        }

        void Hit(Attacker attacker)
        {
            onHit.Invoke(attacker.target);
            attacker.TryHit(new(Blueprint.damage, Blueprint.damageType, this), out _);
        }

        void OnWaveFinished()
        {
            strikes = 0;
        }
    }
}