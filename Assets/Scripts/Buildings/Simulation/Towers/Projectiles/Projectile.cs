using Attackers.Simulation;
using UnityEngine;

namespace Buildings.Simulation.Towers.Projectiles
{
    public abstract class Projectile : MonoBehaviour
    {
        public Tower source;
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
