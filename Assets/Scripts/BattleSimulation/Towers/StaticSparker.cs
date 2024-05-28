using BattleSimulation.Attackers;
using Game.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Towers
{
    public class StaticSparker : Tower
    {
        [Header("References")]
        [SerializeField] Transform sparkOrigin;
        [Header("Settings")]
        [SerializeField] int maxBranches;
        [SerializeField] UnityEvent<(Transform, Attacker)> onShoot;
        [Header("Runtime variables")]
        [SerializeField] protected int shotTimer;

        protected override void FixedUpdateInternal()
        {
            base.FixedUpdateInternal();
            if (!Placed)
                return;

            shotTimer--;
            if (shotTimer > 0)
                return;

            if (targeting.target != null)
                Shoot(targeting.target);
        }

        void Shoot(Attacker primaryTarget)
        {
            shotTimer = Blueprint.interval;
            SoundController.PlaySound(SoundController.Sound.Zap, 0.4f, 1.2f, 0.2f, transform.position);
            var damage = Blueprint.damage;
            ShootOne(sparkOrigin, primaryTarget, damage);

            var potentialSecondaryHits = Physics.SphereCastAll(primaryTarget.target.position + Vector3.down * 5, Blueprint.radius, Vector3.up, 10, LayerMasks.attackerTargets);
            int found = 0;
            for (int upperBound = potentialSecondaryHits.Length; upperBound > 0; upperBound--)
            {
                if (found >= maxBranches)
                    break;
                int r = Random.Range(0, upperBound);
                var potentialHit = potentialSecondaryHits[r];
                (potentialSecondaryHits[r], potentialSecondaryHits[upperBound - 1]) = (potentialSecondaryHits[upperBound - 1], potentialSecondaryHits[r]);
                Attacker a = potentialHit.rigidbody.GetComponent<Attacker>();
                if (a.IsDead || a == primaryTarget)
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