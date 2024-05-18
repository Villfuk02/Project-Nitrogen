using BattleSimulation.Attackers;
using Game.Damage;
using UnityEngine;

namespace BattleSimulation.Towers
{
    public class Sniper : BasicProjectileTower
    {
        protected override Damage GetDamage(Attacker attacker)
        {
            int dmg = Mathf.RoundToInt(Blueprint.damage * (1 + Vector3.Distance(attacker.target.position, targeting.transform.position) / Blueprint.range));
            return new(dmg, Blueprint.damageType, this);
        }
    }
}