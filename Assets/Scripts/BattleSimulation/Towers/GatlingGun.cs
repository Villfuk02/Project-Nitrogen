using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using UnityEngine;
using Utils;

namespace BattleSimulation.Towers
{
    public class GatlingGun : BasicProjectileTower
    {
        [Header("Settings - Gatling Gun")]
        public float baseSpeed;
        public int slowdownRate;
        [Header("Runtime variables - Gatling Gun")]
        public int continuousShootingTicks;
        public int currentInterval;

        protected override void OnInitBlueprint()
        {
            base.OnInitBlueprint();
            continuousShootingTicks = 10000;
            UpdateSpeed();
        }

        protected override void OnPlaced()
        {
            base.OnPlaced();
            WaveController.onWaveFinished.RegisterReaction(ResetSpeed, 1000);
            continuousShootingTicks = 0;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                WaveController.onWaveFinished.UnregisterReaction(ResetSpeed);
        }

        protected override void FixedUpdate()
        {
            if (targeting.target == null)
                continuousShootingTicks = Mathf.Max(continuousShootingTicks - slowdownRate, 0);
            else
                continuousShootingTicks = Mathf.Min(continuousShootingTicks + 1, Blueprint.delay);
            UpdateSpeed();
            base.FixedUpdate();
        }

        protected override void Shoot(Attacker target)
        {
            UpdateSpeed();
            shotTimer = currentInterval;
            ShootInternal(target);
            onShoot.Invoke(target);
        }

        void ResetSpeed()
        {
            continuousShootingTicks = 0;
            UpdateSpeed();
        }

        void UpdateSpeed()
        {
            float speed = Mathf.Lerp(baseSpeed, 1, continuousShootingTicks / (float)Blueprint.delay);
            currentInterval = Mathf.RoundToInt(Blueprint.interval / speed);
        }

        public override IEnumerable<string> GetExtraStats()
        {
            foreach (string s in base.GetExtraStats())
                yield return s;

            yield return $"Interval {TextUtils.FormatTicksStat(TextUtils.Icon.Interval, currentInterval, OriginalBlueprint.interval, true, TextUtils.Improvement.Less)}";
            yield return $"Damage/s {TextUtils.FormatFloatStat(TextUtils.Icon.Dps, Damage.CalculateDps(Blueprint.damage, currentInterval), Damage.CalculateDps(OriginalBlueprint.damage, OriginalBlueprint.interval), true, TextUtils.Improvement.More)}";
        }
    }
}
