using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using Game.Damage;
using UnityEngine;

namespace BattleSimulation.Towers
{
    public class Sniper : BasicProjectileTower
    {
        public override bool Hit(Projectile projectile, Attacker attacker)
        {
            if (attacker.IsDead)
                return false;
            int dmg = Mathf.RoundToInt(Blueprint.damage * (1 + Vector3.Distance(attacker.target.position, targeting.transform.position) / Blueprint.range));
            (Attacker a, Damage dmg) hitParam = (attacker, new(dmg, Blueprint.damageType, this));
            if (!Attacker.HIT.InvokeRef(ref hitParam))
                return false;
            if (hitParam.dmg.amount > 0)
            {
                (Attacker a, Damage dmg) dmgParam = hitParam;
                if (Attacker.DAMAGE.InvokeRef(ref dmgParam))
                    damageDealt += (int)dmgParam.dmg.amount;
            }

            return true;
        }
    }
}
