using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Meteor : Ability
    {
        [Header("References")]
        [SerializeField] Targeting.Targeting targeting;

        [Header("Runtime variables")]
        public int delayLeft;
        bool waveEnded_;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.radius);
            delayLeft = Blueprint.delay;
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
            if (!placed)
                return;
            if (delayLeft == 0 && !waveEnded_)
                Explode();
            delayLeft--;
        }

        void Explode()
        {
            foreach (var a in targeting.GetValidTargets())
                Hit(a);
            Destroy(gameObject, 3f);
        }

        void Hit(Attacker attacker)
        {
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
            if (!Attacker.HIT.InvokeRef(ref hitParam) || hitParam.dmg.amount <= 0)
                return;

            Attacker.DAMAGE.Invoke(hitParam);
        }

        void OnWaveFinished()
        {
            waveEnded_ = true;
            Destroy(gameObject, 3f);
        }
    }
}
