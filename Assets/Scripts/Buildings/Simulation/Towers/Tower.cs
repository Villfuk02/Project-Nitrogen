using Attackers.Simulation;
using Blueprints;
using Buildings.Simulation.Towers.Projectiles;
using UnityEngine;

namespace Buildings.Simulation.Towers
{
    public abstract class Tower : Building
    {
        public Targeting.Targeting targeting;
        public GameObject projectilePrefab;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.Stats[Blueprint.Stat.Range] * 0.1f);
        }

        public abstract void OnHit(Projectile projectile, Attacker attacker);
    }
}
