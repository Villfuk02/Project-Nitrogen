using System.Linq;
using BattleSimulation.Attackers;
using Game.Shared;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace BattleSimulation.Towers
{
    public class UltraRay : Tower
    {
        [Header("Settings")]
        [SerializeField] protected UnityEvent onShoot;
        public int hits;
        [Header("Runtime variables")]
        [SerializeField] protected int shotTimer;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!Placed)
                return;

            shotTimer--;
            if (shotTimer <= 0 && targeting.GetValidTargets().Any())
                Shoot();
        }

        void Shoot()
        {
            shotTimer = currentBlueprint.interval;
            onShoot.Invoke();
            int hitsLeft = hits;
            foreach (var target in targeting.GetValidTargets().OrderBy(LateralDistance))
            {
                if (hitsLeft <= 0)
                    break;
                if (target.TryHit(new(currentBlueprint.damage, currentBlueprint.damageType, this), out var dmg))
                {
                    damageDealt += dmg;
                    SoundController.PlaySound(SoundController.Sound.RayBurn, 0.85f, 1, 0.1f, target.target.position, SoundController.Priority.Low);
                }

                hitsLeft--;
            }
        }

        public float LateralDistance(Attacker a)
        {
            Vector3 diff = a.target.position - targeting.transform.position;
            return diff.XZ().ManhattanMagnitude();
        }
    }
}