using BattleSimulation.Attackers;

namespace BattleSimulation.Projectiles
{
    public interface IProjectileSource
    {
        public bool Hit(Projectile projectile, Attacker attacker);
    }
}
