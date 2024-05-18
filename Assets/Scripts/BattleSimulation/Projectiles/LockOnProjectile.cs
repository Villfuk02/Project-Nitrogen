using System;
using BattleSimulation.Attackers;
using UnityEngine;

namespace BattleSimulation.Projectiles
{
    public class LockOnProjectile : Projectile
    {
        [Header("Settings")]
        public float speed;
        public float maxRange;
        [Header("Runtime values")]
        public Attacker target;
        public Vector3 lastDir;
        public bool hit;

        public void Init(Vector3 position, IProjectileSource source, Attacker target)
        {
            Init(position, source);
            this.target = target;
        }

        void FixedUpdate()
        {
            if (maxRange < 0)
            {
                Destroy(gameObject);
                return;
            }

            maxRange -= speed * Time.fixedDeltaTime;

            if (target != null)
            {
                MoveTowardsTarget();
            }
            else
            {
                target = null;
                transform.Translate(lastDir * (speed * Time.fixedDeltaTime));
            }

            CheckTerrainHit(0.05f);
        }

        void MoveTowardsTarget()
        {
            var position = transform.localPosition;
            var offset = target.target.position - position;
            float distance = offset.magnitude;
            float move = Math.Min(speed * Time.fixedDeltaTime, distance);
            lastDir = offset.normalized;
            transform.Translate(lastDir * move);
        }

        protected override void HitAttacker(Attacker attacker)
        {
            if (hit)
                return;

            if (!source.TryHit(this, attacker))
                return;

            hit = true;
            target = attacker;
        }

        protected override void HitTerrain()
        {
            if (hit || target != null)
                return;

            hit = true;
            lastDir = Vector3.zero;
        }
    }
}