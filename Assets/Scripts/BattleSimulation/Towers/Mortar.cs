using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using BattleSimulation.Towers;
using Utils;

namespace BattleSimulation.Towers
{
    public class Mortar : ProjectileTower
    {
        protected override void ShootInternal(Attacker target)
        {
            var p = Instantiate(projectilePrefab, World.WorldData.World.instance.transform).GetComponent<BallisticProjectile>();
            p.Init(projectileOrigin.position, this, target.target.position, Blueprint.delay * TimeUtils.SECS_PER_TICK, Blueprint.radius);
        }
    }
}
