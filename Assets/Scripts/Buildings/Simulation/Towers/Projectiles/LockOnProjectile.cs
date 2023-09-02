using Attackers.Simulation;
using System;
using UnityEngine;
using Utils;

namespace Buildings.Simulation.Towers.Projectiles
{
    public class LockOnProjectile : Projectile
    {
        [Header("Settings")]
        public float speed;
        public float maxRange;
        [Header("Runtime Values")]
        public Attacker target;
        public Vector3 lastDir;
        public bool hit;
        [SerializeField] bool underground;


        void FixedUpdate()
        {
            if (maxRange < 0)
            {
                Destroy(gameObject);
                return;
            }
            maxRange -= speed * Time.fixedDeltaTime;

            if (target == null)
            {
                target = null;
                transform.Translate(lastDir * (speed * Time.fixedDeltaTime));
            }
            else
            {
                var position = transform.localPosition;
                var offset = target.target.position - position;
                float distance = offset.magnitude;
                float move = Math.Min(speed * Time.fixedDeltaTime, distance);
                lastDir = offset.normalized;
                transform.Translate(lastDir * move);
            }

            var newTilePos = WorldUtils.WorldPosToTilePos(transform.localPosition);
            bool newUnderground = World.World.data.tiles.GetHeightAt(newTilePos) >= newTilePos.z;
            if (newUnderground && !underground)
                HitTerrain();
            underground = newUnderground;
        }

        protected override void HitAttacker(Attacker attacker)
        {
            if (hit)
                return;

            hit = true;
            source.OnHit(this, attacker);
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
