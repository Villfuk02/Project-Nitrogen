using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using Game.Damage;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Towers
{
    public class BasicProjectileTower : Tower
    {
        [SerializeField] Transform projectileOrigin;
        [SerializeField] int shotTimer;
        [SerializeField] UnityEvent onShoot;

        void FixedUpdate()
        {
            if (!placed)
                return;

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
            shotTimer = Blueprint.shotInterval;
            var p = Instantiate(projectilePrefab, World.WorldData.World.instance.transform).GetComponent<LockOnProjectile>();
            p.transform.position = projectileOrigin.position;
            p.source = this;
            p.target = target;
            onShoot.Invoke();
        }

        public override bool Hit(Projectile projectile, Attacker attacker)
        {
            if (attacker.IsDead)
                return false;
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
            if (!Attacker.hit.InvokeRef(ref hitParam))
                return false;
            if (hitParam.dmg.amount > 0)
            {
                (Attacker a, Damage dmg) dmgParam = hitParam;
                if (Attacker.damage.InvokeRef(ref dmgParam))
                    damageDealt += (int)dmgParam.dmg.amount;
            }
            return true;
        }
    }
}
