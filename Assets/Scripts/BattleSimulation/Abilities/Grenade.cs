using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Grenade : Ability
    {
        [Header("References")]
        [SerializeField] Targeting.Targeting targeting;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.radius);
        }

        protected override void OnPlaced()
        {
            Explode();
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
    }
}