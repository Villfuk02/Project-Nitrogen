using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Towers
{
    public class GatlingGun : BasicProjectileTower
    {
        [Header("Settings - Gatling Gun")]
        public float baseSpeed;
        public int slowdownRate;
        [Header("Runtime variables - Gatling Gun")]
        public int continuousShootingTicks;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            Blueprint.Interval.RegisterModifier(UpdateInterval, -1000);
            WaveController.ON_WAVE_FINISHED.RegisterReaction(ResetSpeed, 1000);
            ResetSpeed();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
            {
                Blueprint.Interval.UnregisterModifier(UpdateInterval);
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(ResetSpeed);
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (targeting.target == null)
                continuousShootingTicks = Mathf.Max(continuousShootingTicks - slowdownRate, 0);
            else
                continuousShootingTicks = Mathf.Min(continuousShootingTicks + 1, currentBlueprint.delay);
        }

        void UpdateInterval(IBlueprintProvider provider, ref float interval)
        {
            if (provider is not Blueprinted b || b != this || !Placed)
                return;
            float speed = Mathf.Lerp(baseSpeed, 1, continuousShootingTicks / (float)currentBlueprint.delay);
            interval = Mathf.Max(interval / speed, 1);
        }

        protected override void Shoot(Attacker target)
        {
            shotTimer = currentBlueprint.interval;
            ShootInternal(target);
            onShoot.Invoke(target);
        }

        void ResetSpeed()
        {
            continuousShootingTicks = 0;
        }
    }
}