using Buildings.Simulation.Towers.Projectiles;
using System;
using UnityEngine;

namespace Buildings.Visuals.Towers
{
    public class LockOnProjectileVis : MonoBehaviour
    {
        [SerializeField] LockOnProjectile sim;
        [SerializeField] Vector3 realPos;
        [SerializeField] Vector3 lastDir;
        [SerializeField] float stoppedLifetime;

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
            if (sim.target == null)
            {
                if (sim.hit)
                {
                    stoppedLifetime -= Time.deltaTime;
                    lastDir = Vector3.zero;
                }
                realPos += lastDir * (sim.speed * Time.deltaTime);
            }
            else
            {
                var offset = sim.target.target.position - realPos;
                float distance = offset.magnitude;
                float move = Math.Min(sim.speed * Time.deltaTime, distance);

                if (move <= 0.01f)
                    stoppedLifetime -= Time.deltaTime;

                lastDir = offset.normalized;
                realPos += lastDir * move;
            }
            transform.position = realPos;
        }
    }
}
