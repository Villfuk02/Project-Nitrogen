using BattleSimulation.Control;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Meteor : TargetedAbility
    {
        [Header("Runtime variables")]
        public int delayLeft;
        bool waveEnded_;

        public override void OnSetupChanged()
        {
            base.OnSetupChanged();
            if (!Placed)
                delayLeft = Blueprint.delay;
        }

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
            if (!Placed)
                return;
            if (delayLeft == 0 && !waveEnded_)
                Explode();
            delayLeft--;
        }

        void Explode()
        {
            foreach (var a in targeting.GetValidTargets())
                a.TryHit(new(Blueprint.damage, Blueprint.damageType, this), out _);
            Destroy(gameObject, 3f);
        }

        void OnWaveFinished()
        {
            waveEnded_ = true;
            Destroy(gameObject, 3f);
        }
    }
}