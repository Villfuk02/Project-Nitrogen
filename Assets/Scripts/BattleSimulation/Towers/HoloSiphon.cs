using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
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
            WaveController.onWaveFinished.RegisterReaction(DecrementWaves, 100);

            foreach (var a in targeting.GetValidTargets())
                Hit(a);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                WaveController.onWaveFinished.UnregisterReaction(DecrementWaves);
        }
        void FixedUpdate()
        {
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
                    Hit(selectedTarget);
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

        public void Hit(Attacker attacker)
        {
            if (attacker.IsDead)
                return;
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
            if (!Attacker.HIT.InvokeRef(ref hitParam) || hitParam.dmg.amount <= 0)
                return;
            (Attacker a, Damage dmg) dmgParam = hitParam;
            if (Attacker.DAMAGE.InvokeRef(ref dmgParam))
                damageDealt += (int)dmgParam.dmg.amount;
        }
    }
}
