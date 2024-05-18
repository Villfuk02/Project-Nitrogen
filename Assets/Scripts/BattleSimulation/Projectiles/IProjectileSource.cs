using BattleSimulation.Attackers;

namespace BattleSimulation.Projectiles
{
    public interface IProjectileSource
    {
        public bool TryHit(Projectile projectile, Attacker attacker);
    }
}