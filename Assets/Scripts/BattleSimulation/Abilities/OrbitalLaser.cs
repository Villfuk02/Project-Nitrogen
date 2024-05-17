using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using UnityEngine;
using Utils;

namespace BattleSimulation.Abilities
{
    public class OrbitalLaser : Ability
    {
        [Header("References")]
        [SerializeField] Targeting.Targeting targeting;

        [Header("Settings")]
        [SerializeField] float radius;

        [Header("Runtime variables")]
        public Vector3 startPos;
        public Vector3 endPos;
        public int ticksLeft;
        [SerializeField] bool finished;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(radius);
            ticksLeft = Blueprint.durationTicks;
        }

        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.RegisterReaction(OnWaveFinished, 100);

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

            targeting.transform.position = startPos;
        }

        protected void OnDestroy()
        {
            if (Placed)
                WaveController.onWaveFinished.UnregisterReaction(OnWaveFinished);
        }

        void FixedUpdate()
        {
            if (!Placed)
                return;
            if (ticksLeft == 0)
                End();
            ticksLeft--;
            targeting.transform.position = Vector3.Lerp(startPos, endPos, 1 - ticksLeft / (float)Blueprint.durationTicks);
        }

        public void OnAttackerEnter(Attacker a)
        {
            if (Placed && !finished)
                Hit(a);
        }

        void Hit(Attacker attacker)
        {
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
            if (!Attacker.HIT.InvokeRef(ref hitParam) || hitParam.dmg.amount <= 0)
                return;

            Attacker.DAMAGE.Invoke(hitParam);
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