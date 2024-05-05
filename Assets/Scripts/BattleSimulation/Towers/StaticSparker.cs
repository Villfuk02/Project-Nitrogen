using BattleSimulation.Attackers;
using Game.Damage;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Towers
{
    public class StaticSparker : Tower
    {
        [Header("Settings")]
        [SerializeField] UnityEvent<Attacker> onShoot;

        public void Spark(Attacker attacker)
        {
            if (!placed)
                return;

            if (attacker.IsDead)
                return;
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
            if (!Attacker.HIT.InvokeRef(ref hitParam))
                return;
            if (hitParam.dmg.amount > 0)
            {
                (Attacker a, Damage dmg) dmgParam = hitParam;
                if (Attacker.DAMAGE.InvokeRef(ref dmgParam))
                    damageDealt += (int)dmgParam.dmg.amount;
            }

            onShoot.Invoke(attacker);
        }
    }
}
