using System.Linq;
using Game.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Towers
{
    public class Sledgehammer : Tower
    {
        [Header("Settings")]
        [SerializeField] UnityEvent onShoot;
        [SerializeField] int shotDelay;
        [Header("Runtime variables")]
        [SerializeField] int shotTimer;
        [SerializeField] int shotDelayTimer;

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!Placed)
                return;

            shotTimer--;
            shotDelayTimer--;

            if (shotTimer == 1)
                SoundController.PlaySound(SoundController.Sound.ShootProjectile, 0.75f, 0.5f, 0.2f, transform.position);

            if (shotTimer <= 0 && targeting.GetValidTargets().Any())
                StartShot();

            if (shotDelayTimer == 0)
                Shoot();
        }

        void StartShot()
        {
            shotDelayTimer = shotDelay;
            shotTimer = currentBlueprint.interval;
            onShoot.Invoke();
        }

        void Shoot()
        {
            foreach (var target in targeting.GetValidTargets())
                if (target.TryHit(new(currentBlueprint.damage, currentBlueprint.damageType, this), out var dmg))
                    damageDealt += dmg;
            SoundController.PlaySound(SoundController.Sound.ImpactHuge, 1, 1, 0.2f, transform.position);
        }
    }
}