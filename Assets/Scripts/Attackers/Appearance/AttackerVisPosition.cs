using Assets.Scripts.Attackers.Simulation;
using UnityEngine;

namespace Assets.Scripts.Attackers.Appearance
{
    public class AttackerVisPosition : MonoBehaviour
    {
        [SerializeField] Attacker sim;
        [SerializeField] Vector3 lastTargetPos;
        [SerializeField] float sinceTargetChange;
        [SerializeField] Vector3 lastVelocity;
        [SerializeField] Vector3 realPos;

        private void Start()
        {
            realPos = transform.position;
        }
        void Update()
        {
            if (sim.transform.position != lastTargetPos)
            {
                lastTargetPos = sim.transform.position;
                sinceTargetChange = 0;
            }
            Vector3 velocityNew;
            if (sinceTargetChange == Time.fixedDeltaTime)
                velocityNew = lastVelocity;
            else
                velocityNew = (sim.transform.position - realPos) / (Time.fixedDeltaTime - sinceTargetChange);
            realPos += velocityNew * Time.deltaTime;
            transform.position = realPos;

            lastVelocity = velocityNew;
        }
    }
}

