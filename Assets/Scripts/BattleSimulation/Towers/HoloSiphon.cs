using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using UnityEngine;

namespace BattleSimulation.Towers
{
    public class HoloSiphon : Tower
    {
        [SerializeField] int shotTimer;
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
            if (placed)
                WaveController.onWaveFinished.UnregisterReaction(DecrementWaves);
        }
        void FixedUpdate()
        {
            if (!placed)
                return;

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

            shotTimer--;
            if (shotTimer > 0)
                return;

            targeting.Retarget();
            selectedTarget = targeting.target;
            chargeTimer = 0;
            if (selectedTarget != null)
                shotTimer = Blueprint.interval;
        }

        public override string? GetExtraStats() => $"Damage dealt {damageDealt}\nWaves left {wavesLeft}";

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
            if (!Attacker.hit.InvokeRef(ref hitParam) || hitParam.dmg.amount <= 0)
                return;
            (Attacker a, Damage dmg) dmgParam = hitParam;
            if (Attacker.damage.InvokeRef(ref dmgParam))
                damageDealt += (int)dmgParam.dmg.amount;
        }
    }
}
