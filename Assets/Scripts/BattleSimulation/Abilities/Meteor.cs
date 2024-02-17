using BattleSimulation.Attackers;
using Game.Damage;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Meteor : Ability, IDamageSource
    {
        [SerializeField] Targeting.Targeting targeting;
        public int delayLeft;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.radius);
            delayLeft = Blueprint.delay;
        }

        void FixedUpdate()
        {
            if (!placed)
                return;
            if (delayLeft == 0)
            {
                foreach (var a in targeting.GetValidTargets())
                    Hit(a);
            }
            else if (delayLeft < -40)
            {
                Destroy(gameObject);
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
    }
}
