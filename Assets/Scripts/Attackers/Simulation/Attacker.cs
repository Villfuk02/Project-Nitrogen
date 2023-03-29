using Assets.Scripts.Utils;
using UnityEngine;
using static Assets.Scripts.World.WorldData.WorldData;


namespace Assets.Scripts.Attackers.Simulation
{
    public class Attacker : MonoBehaviour
    {
        [Header("Stats")]
        public float speed;
        [Header("Runtime values")]
        [SerializeField] float pathSegmentProgress;
        [SerializeField] Vector2Int pathSegmentTarget;
        [SerializeField] Vector2Int lastTarget;
        [SerializeField] uint pathSplitIndex;

        private void FixedUpdate()
        {
            pathSegmentProgress += Time.fixedDeltaTime * speed;

            while (pathSegmentProgress >= 1)
            {
                pathSegmentProgress--;
                lastTarget = pathSegmentTarget;
                uint paths = (uint)WORLD_DATA.tiles[pathSegmentTarget].pathNext.Count;
                if (paths == 0)
                {
                    Debug.Log("End of path reached!");
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
            float height = WORLD_DATA.tiles.GetHeightAt(pos) ?? WORLD_DATA.tiles.GetHeightAt(pathSegmentTarget) ?? 0;
            transform.localPosition = WorldUtils.TileToWorldPos(pos.x, pos.y, height);
        }

        public void InitPath(Vector2Int start, Vector2Int firstNode, uint index)
        {
            lastTarget = start;
            pathSegmentTarget = firstNode;
            pathSegmentProgress = 0;
            pathSplitIndex = index;
            UpdateWorldPosition();
        }
    }
}
