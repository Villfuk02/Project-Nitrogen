using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using Game.Shared;
using Utils;

namespace BattleSimulation.Towers
{
    public class Mortar : ProjectileTower
    {
        protected override void ShootInternal(Attacker target)
        {
            var p = Instantiate(projectilePrefab, World.WorldData.World.instance.transform).GetComponent<BallisticProjectile>();
            p.Init(projectileOrigin.position, this, target.target.position, currentBlueprint.delay * TimeUtils.SECS_PER_TICK, currentBlueprint.radius);
            SoundController.PlaySound(SoundController.Sound.ShootHeavy, 0.75f, 1, 0.2f, transform.position);
        }

        protected override void PlayHitSound(Projectile projectile, Attacker attacker, int damage) { }
    }
}