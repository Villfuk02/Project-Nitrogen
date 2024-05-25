using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using Game.Shared;

namespace BattleSimulation.Towers
{
    public class BasicProjectileTower : ProjectileTower
    {
        protected override void ShootInternal(Attacker target)
        {
            var p = Instantiate(projectilePrefab, World.WorldData.World.instance.transform).GetComponent<LockOnProjectile>();
            p.Init(projectileOrigin.position, this, target);
            SoundController.PlaySound(SoundController.Sound.ShootProjectile, 0.35f, 1, 0.2f, projectileOrigin.position, false);
        }
    }
}