using BattleSimulation.Projectiles;
using System;
using UnityEngine;

namespace BattleVisuals.Projectiles
{
    public class LockOnProjectileVis : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] LockOnProjectile sim;
        [SerializeField] float stoppedLifetime;
        [Header("Runtime variables")]
        [SerializeField] Vector3 realPos;
        [SerializeField] Vector3 lastDir;

        void Start()
        {
            realPos = transform.position;
        }

        void Update()
        {
            if (stoppedLifetime < 0)
            {
                Destroy(gameObject);
                return;
            }

            float moveDistance = sim.speed * Time.deltaTime;
            if (sim.target != null)
            {
                var offset = sim.target.target.position - realPos;
                lastDir = offset.normalized;
                float distance = offset.magnitude;
                moveDistance = Math.Min(moveDistance, distance);

                if (moveDistance <= 0.01f)
                    stoppedLifetime -= Time.deltaTime;
            }
            else if (sim.hit)
            {
                stoppedLifetime -= Time.deltaTime;
                lastDir = Vector3.zero;
            }

            realPos += lastDir * moveDistance;
            transform.position = realPos;
        }
    }
}
