using System.Linq;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Shared;
using UnityEngine;
using UnityEngine.Events;
using Utils;
using Random = Utils.Random.Random;

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
        Random random_;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            random_ = BattleController.GetNewRandom();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
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
            shotTimer = currentBlueprint.interval;
            SoundController.PlaySound(SoundController.Sound.Zap, 0.4f, 1.2f, 0.2f, transform.position);
            var damage = currentBlueprint.damage;
            ShootOne(sparkOrigin, primaryTarget, damage);

            var potentialSecondaryHits = Physics.SphereCastAll(primaryTarget.target.position + Vector3.down * 5, currentBlueprint.radius, Vector3.up, 10, LayerMasks.attackerTargets);
            var attackers = potentialSecondaryHits.Select(h => h.rigidbody.GetComponent<Attacker>()).OrderBy(a => a.startPathSplitIndex).ToArray();
            random_.Shuffle(attackers);

            int found = 0;
            foreach (var attacker in attackers)
            {
                if (found >= maxBranches)
                    break;
                if (attacker.IsDead || attacker == primaryTarget)
                    continue;
                found++;
                ShootOne(primaryTarget.target, attacker, damage / 2);
            }
        }

        void ShootOne(Transform from, Attacker target, int baseDamage)
        {
            onShoot.Invoke((from, target));
            bool hit = target.TryHit(new(baseDamage, currentBlueprint.damageType, this), out var dmg);
            if (hit)
                damageDealt += dmg;
        }
    }
}