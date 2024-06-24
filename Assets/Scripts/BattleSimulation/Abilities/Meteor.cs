using BattleSimulation.Control;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Meteor : TargetedAbility
    {
        [Header("Runtime variables")]
        public int delayLeft;
        bool waveEnded_;

        protected override void OnPlaced()
        {
            WaveController.ON_WAVE_FINISHED.RegisterReaction(OnWaveFinished, 100);
        }

        protected void OnDestroy()
        {
            if (Placed)
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(OnWaveFinished);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!Placed)
            {
                delayLeft = currentBlueprint.delay;
                return;
            }

            if (delayLeft == 0 && !waveEnded_)
                Explode();
            delayLeft--;
        }

        void Explode()
        {
            foreach (var a in targeting.GetValidTargets())
                a.TryHit(new(currentBlueprint.damage, currentBlueprint.damageType, this), out _);
            Destroy(gameObject, 3f);
        }

        void OnWaveFinished()
        {
            waveEnded_ = true;
            Destroy(gameObject, 3f);
        }
    }
}