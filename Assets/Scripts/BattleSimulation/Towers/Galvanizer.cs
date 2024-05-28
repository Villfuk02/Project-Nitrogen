using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using BattleSimulation.Projectiles;
using Game.Damage;
using Game.Shared;
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
            ticksSinceLastShot = 1000;
            PlayChargeUpSound();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                WaveController.START_WAVE.UnregisterReaction(OnWaveStarted);
        }

        protected override void FixedUpdateInternal()
        {
            bool prevCharged = IsCharged;
            ticksSinceLastShot++;
            if (Placed && !prevCharged && IsCharged)
                PlayChargeUpSound();
            base.FixedUpdateInternal();
        }

        protected override void ShootInternal(Attacker target)
        {
            var p = Instantiate(IsCharged ? chargedProjectilePrefab : projectilePrefab, World.WorldData.World.instance.transform).GetComponent<LockOnProjectile>();
            if (IsCharged)
                lastChargedProjectile = p;
            ticksSinceLastShot = 0;
            p.Init(projectileOrigin.position, this, target);
            SoundController.PlaySound(SoundController.Sound.ShootProjectile, 0.35f, 1, 0.2f, projectileOrigin.position);
        }

        void OnWaveStarted()
        {
            bool prevCharged = IsCharged;
            ticksSinceLastShot = 1000;
            if (Placed && !prevCharged)
                PlayChargeUpSound();
        }

        void PlayChargeUpSound()
        {
            SoundController.PlaySound(SoundController.Sound.ChargeUp, 0.75f, 1, 0.1f, transform.position);
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
                SoundController.PlaySound(SoundController.Sound.EnergizedImpact, 0.75f, 1, 0.1f, projectile.transform.position);
            }

            int hitDmgDealt = 0;
            if (hitParam.dmg.amount > 0)
            {
                (Attacker a, Damage dmg) dmgParam = hitParam;
                if (Attacker.DAMAGE.InvokeRef(ref dmgParam))
                {
                    hitDmgDealt = (int)dmgParam.dmg.amount;
                    damageDealt += hitDmgDealt;
                }
            }

            PlayHitSound(projectile, attacker, hitDmgDealt);

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