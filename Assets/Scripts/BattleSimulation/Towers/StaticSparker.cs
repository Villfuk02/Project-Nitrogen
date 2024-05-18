using BattleSimulation.Attackers;
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
            if (!Placed)
                return;

            if (!attacker.TryHit(new(Blueprint.damage, Blueprint.damageType, this), out var dmg))
                return;

            onShoot.Invoke(attacker);
            damageDealt += dmg;
        }
    }
}