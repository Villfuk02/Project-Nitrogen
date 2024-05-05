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
        [SerializeField] UnityEvent<Attacker> onShoot;
        [Header("Runtime variables")]
        [SerializeField] int shotTimer;

        void FixedUpdate()
        {
            if (!placed)
                return;

            shotTimer--;
            if (shotTimer > 0)
                return;

            if (targeting.target == null)
                targeting.Retarget();
            if (targeting.target != null)
                Shoot(targeting.target);
        }

        void Shoot(Attacker target)
        {
            shotTimer = Blueprint.interval;
            ShootInternal(target);
            onShoot.Invoke(target);
        }

        protected abstract void ShootInternal(Attacker target);

        public virtual bool Hit(Projectile projectile, Attacker attacker)
        {
            if (attacker.IsDead)
                return false;
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
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
