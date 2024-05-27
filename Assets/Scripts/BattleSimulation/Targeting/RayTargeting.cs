using Game.Shared;
using UnityEngine;

namespace BattleSimulation.Targeting
{
    public class RayTargeting : Targeting
    {
        [Header("References")]
        [SerializeField] BoxCollider col;
        [Header("Settings")]
        [SerializeField] float forwardsOffset;
        [Header("Runtime variables")]
        [SerializeField] float targetRange;
        public float currentRange;
        [SerializeField] Vector3 lastPos;
        [SerializeField] Quaternion lastRot;

        protected override void InitComponents()
        {
            targetingComponent = col.GetComponent<TargetingCollider>();
        }

        public override void SetRange(float range)
        {
            targetRange = range;
            if (checkLineOfSight)
            {
                var position = transform.position;
                Vector3 dir = transform.rotation * Vector3.forward;
                if (Physics.Raycast(position, dir, out RaycastHit hit, range - forwardsOffset, LayerMasks.coarseTerrainAndObstacles))
                    range = hit.distance + forwardsOffset;
            }

            currentRange = range;

            col.size = new(col.size.x, col.size.y, range);
            col.center = Vector3.forward * (range / 2 - forwardsOffset);
        }

        public override bool IsValidTargetPosition(Vector3 pos)
        {
            var p = transform.position;
            var q = transform.rotation;
            if (p != lastPos || q != lastRot)
            {
                lastPos = p;
                lastRot = q;
                SetRange(targetRange);
            }

            return true;
        }
    }
}