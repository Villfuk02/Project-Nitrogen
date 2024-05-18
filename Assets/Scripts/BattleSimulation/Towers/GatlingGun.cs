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

        protected override void OnInit()
        {
            base.OnInit();
            GET_BLUEPRINT.RegisterModifier(UpdateStats, -1000);
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
            GET_BLUEPRINT.UnregisterModifier(UpdateStats);
            if (Placed)
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(ResetSpeed);
        }

        protected override void FixedUpdateInternal()
        {
            base.FixedUpdateInternal();
            if (targeting.target == null)
                continuousShootingTicks = Mathf.Max(continuousShootingTicks - slowdownRate, 0);
            else
                continuousShootingTicks = Mathf.Min(continuousShootingTicks + 1, Blueprint.delay);
        }

        void UpdateStats(ref (Blueprinted blueprinted, Blueprint blueprint) param)
        {
            if (!Placed || param.blueprinted != this)
                return;
            float speed = Mathf.Lerp(baseSpeed, 1, continuousShootingTicks / (float)param.blueprint.delay);
            param.blueprint.interval = Mathf.RoundToInt(param.blueprint.interval / speed);
        }

        protected override void Shoot(Attacker target)
        {
            shotTimer = Blueprint.interval;
            ShootInternal(target);
            onShoot.Invoke(target);
        }

        void ResetSpeed()
        {
            continuousShootingTicks = 0;
        }
    }
}