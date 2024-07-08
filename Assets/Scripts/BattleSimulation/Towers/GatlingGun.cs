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

        static GatlingGun()
        {
            Blueprint.Interval.RegisterModifier(UpdateInterval, -1000);
        }

        protected override void OnPlaced()
        {
            base.OnPlaced();
            WaveController.ON_WAVE_FINISHED.RegisterReaction(ResetSpeed, 1000);
            ResetSpeed();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(ResetSpeed);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (targeting.target == null)
                continuousShootingTicks = Mathf.Max(continuousShootingTicks - slowdownRate, 0);
            else
                continuousShootingTicks = Mathf.Min(continuousShootingTicks + 1, currentBlueprint.delay);
        }

        static void UpdateInterval(IBlueprintProvider provider, ref float interval)
        {
            if (provider is not GatlingGun { Placed: true } g)
                return;
            float speed = Mathf.Lerp(g.baseSpeed, 1, g.continuousShootingTicks / (float)g.currentBlueprint.delay);
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