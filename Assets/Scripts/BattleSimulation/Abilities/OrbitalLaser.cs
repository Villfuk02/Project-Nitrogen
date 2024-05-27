using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Shared;
using UnityEngine;
using Utils;

namespace BattleSimulation.Abilities
{
    public class OrbitalLaser : Ability
    {
        [Header("Settings")]
        [SerializeField] float radius;
        [Header("Runtime variables")]
        public Vector3 startPos;
        public Vector3 endPos;
        public int ticksLeft;
        [SerializeField] bool finished;
        readonly HashSet<Attacker> alreadyHit_ = new();

        public override void OnSetupChanged()
        {
            base.OnSetupChanged();
            if (!Placed)
                ticksLeft = Blueprint.durationTicks;
        }

        protected override void OnPlaced()
        {
            WaveController.ON_WAVE_FINISHED.RegisterReaction(OnWaveFinished, 100);

            Vector3 dir = transform.localRotation * Vector3.forward;
            if (Mathf.Abs(dir.z) <= 1e-4f)
            {
                float startOffset = WorldUtils.WORLD_SIZE.x * 0.5f + 2;
                startPos = new(-dir.x * startOffset, 0, transform.position.z);
                endPos = new(dir.x * startOffset, 0, transform.position.z);
            }
            else
            {
                float startOffset = WorldUtils.WORLD_SIZE.y * 0.5f + 2;
                startPos = new(transform.position.x, 0, -dir.z * startOffset);
                endPos = new(transform.position.x, 0, dir.z * startOffset);
            }
        }

        protected void OnDestroy()
        {
            if (Placed)
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(OnWaveFinished);
        }

        protected override void FixedUpdateInternal()
        {
            base.FixedUpdateInternal();
            if (!Placed || finished)
                return;
            if (ticksLeft == 0)
                End();
            Vector3 prevPosition = Vector3.Lerp(startPos, endPos, 1 - ticksLeft / (float)Blueprint.durationTicks);
            ticksLeft--;
            Vector3 newPosition = Vector3.Lerp(startPos, endPos, 1 - ticksLeft / (float)Blueprint.durationTicks);
            Vector3 offset = newPosition - prevPosition;
            foreach (var hit in Physics.CapsuleCastAll(prevPosition + Vector3.down * 10, prevPosition + Vector3.up * 10, radius, offset, offset.magnitude, LayerMasks.attackerTargets))
            {
                Attacker a = hit.rigidbody.GetComponent<Attacker>();
                if (!alreadyHit_.Add(a))
                    continue;
                if (a.TryHit(new(Blueprint.damage, Blueprint.damageType, this), out _))
                    SoundController.PlaySound(SoundController.Sound.LaserBurn, 1, 1, 0.2f, a.target.position, false);
            }
        }

        void OnWaveFinished()
        {
            End();
        }

        void End()
        {
            finished = true;
            Destroy(gameObject, 2f);
        }
    }
}