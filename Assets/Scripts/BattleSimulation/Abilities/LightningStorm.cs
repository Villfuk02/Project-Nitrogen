using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Abilities
{
    public class LightningStorm : Ability
    {
        [Header("References")]
        [SerializeField] Targeting.Targeting targeting;
        [Header("Settings")]
        [SerializeField] UnityEvent<Transform> onHit;

        [Header("Runtime variables")]
        public int strikes;
        int timer_;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.radius);
            strikes = Blueprint.magic1;
        }
        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.RegisterReaction(OnWaveFinished, 100);
        }
        protected void OnDestroy()
        {
            if (placed)
                WaveController.onWaveFinished.UnregisterReaction(OnWaveFinished);
        }
        void FixedUpdate()
        {
            if (!placed || strikes < 0)
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
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
            if (!Attacker.HIT.InvokeRef(ref hitParam) || hitParam.dmg.amount <= 0)
                return;

            Attacker.DAMAGE.Invoke(hitParam);
        }

        void OnWaveFinished()
        {
            strikes = 0;
        }
    }
}
