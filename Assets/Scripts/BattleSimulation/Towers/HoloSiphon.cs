using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Shared;
using UnityEngine;

namespace BattleSimulation.Towers
{
    public class HoloSiphon : Tower
    {
        [Header("Runtime variables")]
        [SerializeField] int retargetTimer;
        public int chargeTimer;
        [SerializeField] int wavesLeft;
        public Attacker selectedTarget;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            wavesLeft = Blueprint.durationWaves;
            WaveController.ON_WAVE_FINISHED.RegisterReaction(DecrementWaves, 100);

            foreach (var a in targeting.GetValidTargets())
                if (a.TryHit(new(Blueprint.damage, Blueprint.damageType, this), out var dmg))
                {
                    SoundController.PlaySound(SoundController.Sound.SiphonFinish, 0.45f, 1, 0.2f, a.target.position, false);
                    damageDealt += dmg;
                }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(DecrementWaves);
        }

        protected override void FixedUpdateInternal()
        {
            base.FixedUpdateInternal();
            if (!Placed)
                return;

            UpdateCharge();
            UpdateTarget();
        }

        void UpdateTarget()
        {
            retargetTimer--;
            if (retargetTimer > 0)
                return;

            targeting.Retarget();
            selectedTarget = targeting.target;
            chargeTimer = 0;
            if (selectedTarget != null)
                retargetTimer = Blueprint.interval;
        }

        void UpdateCharge()
        {
            if (targeting.IsInRangeAndValid(selectedTarget))
            {
                chargeTimer++;
                if (chargeTimer >= Blueprint.delay)
                {
                    if (selectedTarget.TryHit(new(Blueprint.damage, Blueprint.damageType, this), out var dmg))
                    {
                        SoundController.PlaySound(SoundController.Sound.SiphonFinish, 0.6f, 1, 0.2f, selectedTarget.target.position, false);
                        damageDealt += dmg;
                    }

                    selectedTarget = null;
                }
            }
            else
            {
                selectedTarget = null;
                chargeTimer = 0;
            }
        }

        public override IEnumerable<string> GetExtraStats()
        {
            if (Placed)
                yield return $"Waves left [#DUR]{wavesLeft}";

            foreach (string s in base.GetExtraStats())
                yield return s;
        }

        public void DecrementWaves()
        {
            wavesLeft--;
            if (wavesLeft <= 0)
                Delete();
        }
    }
}