using UnityEngine;
using Utils;

namespace Attackers.Simulation
{
    public class Attacker : MonoBehaviour
    {
        [SerializeField] Rigidbody rb;
        [Header("Constants")]
        public const float SMALL_TARGET_HEIGHT = 0.3f;
        public const float LARGE_TARGET_HEIGHT = 0.6f;
        [Header("Stats")]
        public float speed;
        [Header("Runtime References")]
        public Transform target;
        [Header("Runtime values")]
        [SerializeField] float pathSegmentProgress;
        [SerializeField] Vector2Int pathSegmentTarget;
        [SerializeField] Vector2Int lastTarget;
        [SerializeField] uint pathSplitIndex;
        [SerializeField] int segmentsToCenter;

        void FixedUpdate()
        {
            pathSegmentProgress += Time.fixedDeltaTime * speed;

            while (pathSegmentProgress >= 1)
            {
                pathSegmentProgress--;
                segmentsToCenter--;
                lastTarget = pathSegmentTarget;
                uint paths = (uint)World.World.data.tiles[pathSegmentTarget].pathNext.Count;
                if (paths == 0)
                {
                    Destroy(gameObject);
                    return;
                }
                uint chosen = pathSplitIndex % paths;
                pathSplitIndex /= paths;
                pathSegmentTarget = World.World.data.tiles[pathSegmentTarget].pathNext[(int)chosen].pos;
            }

            UpdateWorldPosition();
        }

        void UpdateWorldPosition()
        {
            Vector2 pos = Vector2.Lerp(lastTarget, pathSegmentTarget, pathSegmentProgress);
            float height = World.World.data.tiles.GetHeightAt(pos) ?? World.World.data.tiles.GetHeightAt(pathSegmentTarget)!.Value;
            Vector3 worldPos = WorldUtils.TilePosToWorldPos(pos.x, pos.y, height);
            rb.MovePosition(worldPos);
            transform.position = worldPos;
        }

        public void InitPath(Vector2Int start, Vector2Int firstNode, uint index)
        {
            lastTarget = start;
            pathSegmentTarget = firstNode;
            pathSegmentProgress = 0;
            pathSplitIndex = index;
            segmentsToCenter = World.World.data.tiles[firstNode].dist + 1;
            transform.localPosition = Vector3.up * 200;
            UpdateWorldPosition();
        }

        public float GetDistanceToCenter()
        {
            return segmentsToCenter - pathSegmentProgress;
        }

        void OnDrawGizmosSelected()
        {
            if (!World.World.instance.Ready)
                return;
            float height = World.World.data.tiles.GetHeightAt(pathSegmentTarget) ?? 0;
            Vector3 segTarget = WorldUtils.TilePosToWorldPos(pathSegmentTarget.x, pathSegmentTarget.y, height);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(segTarget, Vector3.one * 0.15f);
            Gizmos.DrawLine(transform.position, segTarget);
        }
    }
}
