using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using UnityEngine;

namespace BattleSimulation.Towers
{
    public class DoubleSentry : BasicProjectileTower
    {
        [Header("References")]
        [SerializeField] Transform projectileOrigin2;
        [Header("Runtime variables")]
        [SerializeField] bool useSecondBarrel;

        protected override void ShootInternal(Attacker target)
        {
            var p = Instantiate(projectilePrefab, World.WorldData.World.instance.transform).GetComponent<LockOnProjectile>();
            p.Init((useSecondBarrel ? projectileOrigin2 : projectileOrigin).position, this, target);
            useSecondBarrel = !useSecondBarrel;
        }
    }
}