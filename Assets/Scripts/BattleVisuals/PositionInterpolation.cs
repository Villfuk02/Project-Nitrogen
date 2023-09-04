using UnityEngine;

namespace BattleVisuals
{
    public class PositionInterpolation : MonoBehaviour
    {
        [SerializeField] Transform sim;
        [SerializeField] Vector3 lastTargetPos;
        [SerializeField] float sinceTargetChange;
        [SerializeField] Vector3 lastVelocity;
        [SerializeField] Vector3 realPos;

        void Start()
        {
            realPos = transform.position;
        }
        void Update()
        {
            if (sim.position != lastTargetPos)
            {
                lastTargetPos = sim.position;
                sinceTargetChange = 0;
            }
            Vector3 velocityNew;
            if (sinceTargetChange == Time.fixedDeltaTime)
                velocityNew = lastVelocity;
            else
                velocityNew = (sim.position - realPos) / (Time.fixedDeltaTime - sinceTargetChange);
            realPos += velocityNew * Time.deltaTime;

            transform.position = realPos;
            lastVelocity = velocityNew;
        }
    }
}
