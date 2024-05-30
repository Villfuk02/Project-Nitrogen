using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using Game.Damage;
using Game.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Towers
{
    public abstract class ProjectileTower : Tower, IProjectileSource
    {
        [Header("References")]
        [SerializeField] protected Transform projectileOrigin;
        [Header("Settings")]
        public GameObject projectilePrefab;
        [SerializeField] protected UnityEvent<Attacker> onShoot;
        [Header("Runtime variables")]
        public int shotTimer;

        protected override void FixedUpdateInternal()
        {
            base.FixedUpdateInternal();
            if (!Placed)
                return;

            shotTimer--;
            if (shotTimer > 0)
                return;

            if (targeting.target != null)
                Shoot(targeting.target);
        }

        protected virtual void Shoot(Attacker target)
        {
            shotTimer = Blueprint.interval;
            ShootInternal(target);
            onShoot.Invoke(target);
        }

        protected abstract void ShootInternal(Attacker target);

        protected virtual Damage GetDamage(Attacker attacker) => new(Blueprint.damage, Blueprint.damageType, this);

        protected virtual void PlayHitSound(Projectile projectile, Attacker attacker, int damage)
        {
            float volume;
            SoundController.Sound sound;
            switch (damage)
            {
                case 0:
                    sound = SoundController.Sound.Clink;
                    volume = 0.6f;
                    break;
                case < 10:
                    sound = SoundController.Sound.ImpactSmall;
                    volume = Mathf.Lerp(0.45f, 0.75f, damage / 10f);
                    break;
                default:
                    sound = SoundController.Sound.ImpactBig;
                    volume = Mathf.Lerp(0.6f, 0.75f, (damage - 10) / 20f);
                    break;
            }

            SoundController.PlaySound(sound, volume, 1, 0.2f, projectile.transform.position);
        }

        public virtual bool TryHit(Projectile projectile, Attacker attacker)
        {
            bool hit = attacker.TryHit(GetDamage(attacker), out var dmg);
            if (hit)
            {
                damageDealt += dmg;
                PlayHitSound(projectile, attacker, dmg);
            }

            return hit;
        }
    }
}