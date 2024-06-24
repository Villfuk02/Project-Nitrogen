using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Blueprint;
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

        protected override void OnInit()
        {
            base.OnInit();
            baseBlueprint.type = Blueprint.Type.Tower;
        }

        protected override void OnPlaced()
        {
            base.OnPlaced();
            wavesLeft = currentBlueprint.durationWaves;
            WaveController.ON_WAVE_FINISHED.RegisterReaction(DecrementWaves, 100);

            foreach (var a in targeting.GetValidTargets())
            {
                if (a.TryHit(new(currentBlueprint.damage, currentBlueprint.damageType, this), out var dmg))
                {
                    SoundController.PlaySound(SoundController.Sound.SiphonFinish, 0.45f, 1, 0.2f, a.target.position);
                    damageDealt += dmg;
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(DecrementWaves);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
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

            selectedTarget = targeting.target;
            chargeTimer = 0;
            if (selectedTarget != null)
                retargetTimer = currentBlueprint.interval;
        }

        void UpdateCharge()
        {
            if (targeting.IsInRangeAndValid(selectedTarget))
            {
                chargeTimer++;
                if (chargeTimer >= currentBlueprint.delay)
                {
                    if (selectedTarget.TryHit(new(currentBlueprint.damage, currentBlueprint.damageType, this), out var dmg))
                    {
                        SoundController.PlaySound(SoundController.Sound.SiphonFinish, 0.6f, 1, 0.2f, selectedTarget.target.position);
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