using System.Linq;
using Game.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Towers
{
    public class Sledgehammer : Tower
    {
        [Header("Settings")]
        [SerializeField] protected UnityEvent onShoot;
        [SerializeField] protected int shotDelay;
        [Header("Runtime variables")]
        [SerializeField] protected int shotTimer;
        [SerializeField] protected int shotDelayTimer;

        protected override void OnPlaced()
        {
            shotDelayTimer = shotDelay + 1;
            base.OnPlaced();
        }

        protected override void FixedUpdateInternal()
        {
            base.FixedUpdateInternal();
            if (!Placed)
                return;

            shotTimer--;
            shotDelayTimer++;

            if (shotTimer == 1)
                SoundController.PlaySound(SoundController.Sound.ShootProjectile, 0.75f, 0.5f, 0.2f, transform.position);

            if (shotTimer <= 0 && targeting.GetValidTargets().Any())
                StartShot();

            if (shotDelayTimer == shotDelay)
                Shoot();
        }

        void StartShot()
        {
            shotDelayTimer = 0;
            shotTimer = Blueprint.interval;
            onShoot.Invoke();
        }

        void Shoot()
        {
            foreach (var target in targeting.GetValidTargets())
                if (target.TryHit(new(Blueprint.damage, Blueprint.damageType, this), out var dmg))
                    damageDealt += dmg;
            SoundController.PlaySound(SoundController.Sound.ImpactHuge, 1, 1, 0.2f, transform.position);
        }
    }
}