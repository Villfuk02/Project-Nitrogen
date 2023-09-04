using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using UnityEngine;

namespace BattleSimulation.Towers
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
            }
        }

        void Shoot(Attacker target)
        {
            shotTimer = Blueprint.shotInterval!.Value;
            var p = Instantiate(projectilePrefab, World.WorldData.World.instance.transform).GetComponent<LockOnProjectile>();
            p.transform.position = projectileOrigin.position;
            p.source = this;
            p.target = target;
        }

        public override void OnHit(Projectile projectile, Attacker attacker)
        {
            attacker.TakeDamage(new(Blueprint.damage!.Value, Blueprint.damageType!.Value, this));
        }
    }
}
