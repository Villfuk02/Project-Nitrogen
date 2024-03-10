using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using Game.Damage;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Towers
{
    public class BasicProjectileTower : Tower, IProjectileSource
    {
        [Header("References")]
        [SerializeField] Transform projectileOrigin;
        [Header("Settings")]
        public GameObject projectilePrefab;
        [SerializeField] UnityEvent onShoot;
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
            var p = Instantiate(projectilePrefab, World.WorldData.World.instance.transform).GetComponent<LockOnProjectile>();
            p.transform.position = projectileOrigin.position;
            p.source = this;
            p.target = target;
            onShoot.Invoke();
        }

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
