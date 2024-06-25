using BattleSimulation.Attackers;
using Game.Shared;
using UnityEngine;
using Utils;

namespace BattleSimulation.Towers
{
    public class Sniper : BasicProjectileTower
    {
        protected override Damage GetDamage(Attacker attacker)
        {
            int dmg = Mathf.RoundToInt(currentBlueprint.damage * (1 + Vector2.Distance(attacker.target.position.XZ(), targeting.transform.position.XZ()) / currentBlueprint.range));
            return new(dmg, currentBlueprint.damageType, this);
        }
    }
}