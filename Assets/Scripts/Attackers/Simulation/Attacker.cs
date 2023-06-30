
using UnityEngine;
using Utils;
using static World.WorldData.WorldData;


namespace Attackers.Simulation
{
    public class Attacker : MonoBehaviour
    {
        [Header("Constants")]
        public const float SMALL_TARGET_HEIGHT = 0.3f;
        public const float LARGE_TARGET_HEIGHT = 0.6f;
        [Header("References")]
        public Transform target;
        [Header("Stats")]
        public float speed;
        [Header("Runtime values")]
        [SerializeField] float pathSegmentProgress;
        [SerializeField] Vector2Int pathSegmentTarget;
        [SerializeField] Vector2Int lastTarget;
        [SerializeField] uint pathSplitIndex;
        [SerializeField] int segmentsToCenter;

        private void FixedUpdate()
        {
            pathSegmentProgress += Time.fixedDeltaTime * speed;

            while (pathSegmentProgress >= 1)
            {
                pathSegmentProgress--;
                segmentsToCenter--;
                lastTarget = pathSegmentTarget;
                uint paths = (uint)WORLD_DATA.tiles[pathSegmentTarget].pathNext.Count;
                if (paths == 0)
                {
                    Destroy(gameObject);
                    return;
                }
                uint chosen = pathSplitIndex % paths;
                pathSplitIndex /= paths;
                pathSegmentTarget = WORLD_DATA.tiles[pathSegmentTarget].pathNext[(int)chosen].pos;
            }

            UpdateWorldPosition();
        }

        private void UpdateWorldPosition()
        {
            Vector2 pos = Vector2.Lerp(lastTarget, pathSegmentTarget, pathSegmentProgress);
            float height = WORLD_DATA.tiles.GetHeightAt(pos) ?? WORLD_DATA.tiles.GetHeightAt(pathSegmentTarget) ?? -3;
            transform.localPosition = WorldUtils.TileToWorldPos(pos.x, pos.y, height);
        }

        public void InitPath(Vector2Int start, Vector2Int firstNode, uint index)
        {
            lastTarget = start;
            pathSegmentTarget = firstNode;
            pathSegmentProgress = 0;
            pathSplitIndex = index;
            segmentsToCenter = WORLD_DATA.tiles[firstNode].dist + 1;
            UpdateWorldPosition();
        }

        public float GetDistanceToCenter()
        {
            return segmentsToCenter - pathSegmentProgress;
        }

        private void OnDrawGizmosSelected()
        {
            if (WORLD_DATA == null)
                return;
            float height = WORLD_DATA.tiles.GetHeightAt(pathSegmentTarget) ?? 0;
            Vector3 segTarget = WorldUtils.TileToWorldPos(pathSegmentTarget.x, pathSegmentTarget.y, height);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(segTarget, Vector3.one * 0.15f);
            Gizmos.DrawLine(transform.position, segTarget);
        }
    }
}
