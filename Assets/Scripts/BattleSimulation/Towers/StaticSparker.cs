using BattleSimulation.Attackers;
using Game.Shared;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace BattleSimulation.Towers
{
    public class StaticSparker : Tower
    {
        static LayerMask attackerMask_;

        [Header("Settings")]
        [SerializeField] int maxBranches;
        [SerializeField] UnityEvent<(Transform, Attacker)> onShoot;
        [Header("Runtime variables")]
        [SerializeField] protected int shotTimer;

        void Awake()
        {
            if (attackerMask_ == 0)
                attackerMask_ = LayerMask.GetMask(LayerNames.ATTACKER_TARGET);
        }

        protected override void FixedUpdateInternal()
        {
            base.FixedUpdateInternal();
            if (!Placed)
                return;

            shotTimer--;
            if (shotTimer > 0)
                return;

            if (targeting.target == null)
                targeting.Retarget();
            if (targeting.target != null)
                Shoot(targeting.target);
        }

        void Shoot(Attacker primaryTarget)
        {
            shotTimer = Blueprint.interval;
            SoundController.PlaySound(SoundController.Sound.Zap, 0.4f, 1.2f, 0.2f, transform.position, false);
            var damage = Blueprint.damage;
            ShootOne(targeting.transform, primaryTarget, damage);

            var potentialSecondaryHits = Physics.SphereCastAll(primaryTarget.target.position + Vector3.down * 5, Blueprint.radius, Vector3.up, 10, attackerMask_);
            int found = 0;
            for (int upperBound = potentialSecondaryHits.Length; upperBound > 0; upperBound--)
            {
                if (found >= maxBranches)
                    break;
                int r = Random.Range(0, upperBound);
                var potentialHit = potentialSecondaryHits[r];
                (potentialSecondaryHits[r], potentialSecondaryHits[upperBound - 1]) = (potentialSecondaryHits[upperBound - 1], potentialSecondaryHits[r]);
                Attacker a = potentialHit.rigidbody.GetComponent<Attacker>();
                if (a == primaryTarget)
                    continue;
                found++;
                ShootOne(primaryTarget.target, a, damage / 2);
            }
        }

        void ShootOne(Transform from, Attacker target, int baseDamage)
        {
            onShoot.Invoke((from, target));
            bool hit = target.TryHit(new(baseDamage, Blueprint.damageType, this), out var dmg);
            if (hit)
                damageDealt += dmg;
        }
    }
}