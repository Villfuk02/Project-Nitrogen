using BattleSimulation.Attackers;
using Game.Shared;
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
            SoundController.PlaySound(SoundController.Sound.Zap, 0.4f, 1.2f, 0.2f, transform.position, false);
        }
    }
}