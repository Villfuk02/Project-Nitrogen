using BattleSimulation.Attackers;

namespace BattleSimulation.Projectiles
{
    public interface IProjectileSource
    {
        public void OnHit(Projectile projectile, Attacker attacker);
    }
}
