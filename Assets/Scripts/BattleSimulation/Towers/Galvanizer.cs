using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using BattleSimulation.Projectiles;
using Game.Damage;
using UnityEngine;
using Utils;

namespace BattleSimulation.Towers
{
    public class Galvanizer : ProjectileTower
    {
        [Header("References")]
        [SerializeField] GameObject chargedProjectilePrefab;
        [Header("Settings")]
        [SerializeField] int additionalDmg;
        [Header("Runtime variables")]
        [SerializeField] int ticksSinceLastShot;
        [SerializeField] Projectile lastChargedProjectile;
        [SerializeField] int energyProduced;

        bool IsCharged => ticksSinceLastShot >= Blueprint.delay;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            WaveController.START_WAVE.RegisterReaction(OnWaveStarted, 100);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                WaveController.START_WAVE.UnregisterReaction(OnWaveStarted);
        }

        protected override void FixedUpdateInternal()
        {
            ticksSinceLastShot++;
            base.FixedUpdateInternal();
        }

        protected override void ShootInternal(Attacker target)
        {
            var p = Instantiate(IsCharged ? chargedProjectilePrefab : projectilePrefab, World.WorldData.World.instance.transform).GetComponent<LockOnProjectile>();
            ticksSinceLastShot = 0;
            if (IsCharged)
                lastChargedProjectile = p;
            p.Init(projectileOrigin.position, this, target);
        }

        void OnWaveStarted()
        {
            ticksSinceLastShot = 1000;
        }

        public override bool TryHit(Projectile projectile, Attacker attacker)
        {
            if (attacker.IsDead)
                return false;
            bool charged = projectile == lastChargedProjectile;
            Damage dmg = new(Blueprint.damage, Blueprint.damageType, this);
            if (charged)
            {
                dmg.amount += additionalDmg;
                dmg.type |= Damage.Type.Energy;
            }

            (Attacker a, Damage dmg) hitParam = (attacker, dmg);
            if (!Attacker.HIT.InvokeRef(ref hitParam))
                return false;
            if (charged)
            {
                (object source, float amount) energyProductionParam = (this, Blueprint.energyProduction);
                if (BattleController.ADD_ENERGY.InvokeRef(ref energyProductionParam))
                    energyProduced += (int)energyProductionParam.amount;
            }

            if (hitParam.dmg.amount > 0)
            {
                (Attacker a, Damage dmg) dmgParam = hitParam;
                if (Attacker.DAMAGE.InvokeRef(ref dmgParam))
                    damageDealt += (int)dmgParam.dmg.amount;
            }

            return true;
        }

        public override IEnumerable<string> GetExtraStats()
        {
            if (Placed)
            {
                if (IsCharged)
                    yield return $"{TextUtils.Icon.Energy.Sprite()} Charged!";
                yield return $"Produced [#ENE]{energyProduced}";
            }

            foreach (string s in base.GetExtraStats())
                yield return s;
        }
    }
}