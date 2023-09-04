using BattleSimulation.Attackers;
using UnityEngine;

namespace BattleSimulation.Projectiles
{
    public abstract class Projectile : MonoBehaviour
    {
        public IProjectileSource source;
        protected abstract void HitAttacker(Attacker attacker);
        protected abstract void HitTerrain();

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Attacker>(out var attacker))
                HitAttacker(attacker);
            else
                HitTerrain();
        }
    }
}
