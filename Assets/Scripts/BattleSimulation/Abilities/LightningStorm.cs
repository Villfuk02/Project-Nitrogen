using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Abilities
{
    public class LightningStorm : Ability, IDamageSource
    {
        [SerializeField] Targeting.Targeting targeting;
        [SerializeField] UnityEvent<Transform> onHit;
        int timer_;
        public int strikes;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.radius);
            strikes = Blueprint.magic1;
        }
        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.Register(OnWaveFinished, 100);
        }
        protected void OnDestroy()
        {
            if (placed)
                WaveController.onWaveFinished.Unregister(OnWaveFinished);
        }
        void FixedUpdate()
        {
            if (!placed)
                return;
            if (strikes == 0)
            {
                strikes--;
                Destroy(gameObject, 5f);
                return;
            }
            if (timer_ == 0 && strikes > 0)
            {
                timer_ = Blueprint.interval;
                strikes--;
                var targets = targeting.GetValidTargets().Where(a => !a.IsDead).ToArray();
                if (targets.Length > 0)
                    Hit(targets[Random.Range(0, targets.Length)]);
            }
            timer_--;
        }

        void Hit(Attacker attacker)
        {
            onHit.Invoke(attacker.target);
            if (attacker.IsDead)
                return;
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
            if (!Attacker.hit.InvokeRef(ref hitParam) || hitParam.dmg.amount <= 0)
                return;

            Attacker.damage.Invoke(hitParam);
        }

        void OnWaveFinished()
        {
            strikes = 0;
        }
    }
}
