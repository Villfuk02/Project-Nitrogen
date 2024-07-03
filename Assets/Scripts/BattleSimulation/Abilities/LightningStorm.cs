using System.Linq;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using UnityEngine;
using UnityEngine.Events;
using Random = Utils.Random.Random;

namespace BattleSimulation.Abilities
{
    public class LightningStorm : TargetedAbility
    {
        [Header("Settings")]
        [SerializeField] UnityEvent<Transform> onHit;
        public int strikes;
        [Header("Runtime variables")]
        int timer_;
        Random random_;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            WaveController.ON_WAVE_FINISHED.RegisterReaction(OnWaveFinished, 100);
            random_ = PlayerState.GetNewRandom();
        }

        protected void OnDestroy()
        {
            if (Placed)
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(OnWaveFinished);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

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
            timer_ = currentBlueprint.interval;
            var targets = targeting.GetValidTargets().ToArray();
            if (targets.Length <= 0)
                return;
            targets = targets.OrderBy(a => a.startPathSplitIndex).ToArray();
            Hit(targets[random_.Int(targets.Length)]);
        }

        void Hit(Attacker attacker)
        {
            onHit.Invoke(attacker.target);
            attacker.TryHit(new(currentBlueprint.damage, currentBlueprint.damageType, this), out _);
        }

        void OnWaveFinished()
        {
            strikes = 0;
        }
    }
}