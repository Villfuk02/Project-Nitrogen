using UnityEngine;
using Utils;
using World.WorldData;

namespace Attackers.Simulation
{
    public class Attacker : MonoBehaviour
    {
        [Header("Constants")]
        public const float SMALL_TARGET_HEIGHT = 0.3f;
        public const float LARGE_TARGET_HEIGHT = 0.6f;
        [Header("Stats")]
        public float speed;
        [Header("Runtime References")]
        public WorldData worldData;
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
                uint paths = (uint)worldData.tiles[pathSegmentTarget].pathNext.Count;
                if (paths == 0)
                {
                    Destroy(gameObject);
                    return;
                }
                uint chosen = pathSplitIndex % paths;
                pathSplitIndex /= paths;
                pathSegmentTarget = worldData.tiles[pathSegmentTarget].pathNext[(int)chosen].pos;
            }

            UpdateWorldPosition();
        }

        void UpdateWorldPosition()
        {
            Vector2 pos = Vector2.Lerp(lastTarget, pathSegmentTarget, pathSegmentProgress);
            float height = worldData.tiles.GetHeightAt(pos) ?? worldData.tiles.GetHeightAt(pathSegmentTarget)!.Value;
            transform.localPosition = WorldUtils.TileToWorldPos(pos.x, pos.y, height);
        }

        public void InitPath(Vector2Int start, Vector2Int firstNode, uint index)
        {
            lastTarget = start;
            pathSegmentTarget = firstNode;
            pathSegmentProgress = 0;
            pathSplitIndex = index;
            segmentsToCenter = worldData.tiles[firstNode].dist + 1;
            UpdateWorldPosition();
        }

        public float GetDistanceToCenter()
        {
            return segmentsToCenter - pathSegmentProgress;
        }

        void OnDrawGizmosSelected()
        {
            if (worldData == null)
                return;
            float height = worldData.tiles.GetHeightAt(pathSegmentTarget) ?? 0;
            Vector3 segTarget = WorldUtils.TileToWorldPos(pathSegmentTarget.x, pathSegmentTarget.y, height);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(segTarget, Vector3.one * 0.15f);
            Gizmos.DrawLine(transform.position, segTarget);
        }
    }
}
