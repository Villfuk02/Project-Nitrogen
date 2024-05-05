using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;

namespace BattleSimulation.Towers
{
    public class BasicProjectileTower : ProjectileTower
    {
        protected override void ShootInternal(Attacker target)
        {
            var p = Instantiate(projectilePrefab, World.WorldData.World.instance.transform).GetComponent<LockOnProjectile>();
            p.Init(projectileOrigin.position, this, target);
        }
    }
}
