using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using BattleSimulation.Towers;

namespace BattleSimulation.Towers
{
    public class Mortar : ProjectileTower
    {
        protected override void ShootInternal(Attacker target)
        {
            var p = Instantiate(projectilePrefab, World.WorldData.World.instance.transform).GetComponent<BallisticProjectile>();
            p.Init(projectileOrigin.position, this, target.target.position, Blueprint.delay * 0.05f, Blueprint.radius);
        }
    }
}
