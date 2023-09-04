using BattleSimulation.Attackers;
using BattleSimulation.Buildings;
using BattleSimulation.Projectiles;
using Game.Damage;
using UnityEngine;

namespace BattleSimulation.Towers
{
    public abstract class Tower : Building, IDamageSource, IProjectileSource
    {
        public Targeting.Targeting targeting;
        public GameObject projectilePrefab;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.range!.Value * 0.1f);
        }

        public abstract void OnHit(Projectile projectile, Attacker attacker);
    }
}
