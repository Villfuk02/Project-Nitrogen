using Attackers.Simulation;
using Blueprints;
using Buildings.Simulation.Towers.Projectiles;
using UnityEngine;

namespace Buildings.Simulation.Towers
{
    public class BasicProjectileTower : Tower
    {
        [SerializeField] Transform projectileOrigin;
        [SerializeField] int shotTimer;

        void FixedUpdate()
        {
            if (shotTimer > 0)
            {
                shotTimer--;
                return;
            }

            targeting.Retarget();
            if (targeting.target != null)
            {
                Shoot(targeting.target);
                return;
            }
        }

        void Shoot(Attacker target)
        {
            shotTimer = Blueprint.Stats[Blueprint.Stat.ShotInterval];
            var p = Instantiate(projectilePrefab, World.World.instance.transform).GetComponent<LockOnProjectile>();
            p.transform.position = projectileOrigin.position;
            p.source = this;
            p.target = target;
        }

        public override void OnHit(Projectile projectile, Attacker attacker)
        {
            attacker.TakeDamage(new(Blueprint.Stats[Blueprint.Stat.Damage], (Damage.Type)Blueprint.Stats[Blueprint.Stat.DamageType]));
        }
    }
}
