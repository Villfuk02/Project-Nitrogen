using BattleSimulation.Attackers;
using System;
using UnityEngine;
using Utils;

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

            CheckTerrain();
        }

        void CheckTerrain()
        {
            var newTilePos = WorldUtils.WorldPosToTilePos(transform.localPosition);
            bool underground = World.WorldData.World.data.tiles.GetHeightAt(newTilePos) >= newTilePos.z;
            if (underground && !hit)
                HitTerrain();
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

            if (!source.Hit(this, attacker))
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
