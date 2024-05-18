using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using Game.Damage;
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
        [SerializeField] protected int shotTimer;

        protected override void FixedUpdateInternal()
        {
            base.FixedUpdateInternal();
            if (!Placed)
                return;

            shotTimer--;
            if (shotTimer > 0)
                return;

            if (targeting.target == null)
                targeting.Retarget();
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

        public virtual bool TryHit(Projectile projectile, Attacker attacker)
        {
            bool hit = attacker.TryHit(GetDamage(attacker), out var dmg);
            if (hit)
                damageDealt += dmg;
            return hit;
        }
    }
}