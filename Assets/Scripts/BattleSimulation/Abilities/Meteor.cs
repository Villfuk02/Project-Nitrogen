using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Meteor : Ability, IDamageSource
    {
        [SerializeField] Targeting.Targeting targeting;
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
            {
                foreach (var a in targeting.GetValidTargets())
                    Hit(a);
                Destroy(gameObject, 3f);
            }
            delayLeft--;
        }

        void Hit(Attacker attacker)
        {
            if (attacker.IsDead)
                return;
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
            if (!Attacker.hit.InvokeRef(ref hitParam) || hitParam.dmg.amount <= 0)
                return;

            Attacker.damage.Invoke(hitParam);
        }

        void OnWaveFinished()
        {
            waveEnded_ = true;
        }
    }
}
