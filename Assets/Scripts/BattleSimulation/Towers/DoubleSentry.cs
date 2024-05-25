using BattleSimulation.Attackers;
using BattleSimulation.Projectiles;
using Game.Shared;
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
            var origin = (useSecondBarrel ? projectileOrigin2 : projectileOrigin).position;
            p.Init(origin, this, target);
            SoundController.PlaySound(SoundController.Sound.ShootProjectile, 0.35f, 1, 0.2f, origin, false);
            useSecondBarrel = !useSecondBarrel;
        }
    }
}