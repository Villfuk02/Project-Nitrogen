using BattleSimulation.Projectiles;
using UnityEngine;

namespace BattleVisuals.Projectiles
{
    public class BallisticProjectileVis : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BallisticProjectile sim;
        [Header("Runtime variables")]
        [SerializeField] Vector3 realPos;
        [SerializeField] Vector3 velocity;

        void Start()
        {
            velocity = sim.velocity;
            realPos = transform.position;
        }

        void Update()
        {
            realPos += velocity * Time.deltaTime;
            velocity += Physics.gravity * Time.deltaTime;
            transform.position = realPos;
        }
    }
}
