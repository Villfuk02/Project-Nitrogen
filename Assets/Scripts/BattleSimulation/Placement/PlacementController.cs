using BattleSimulation.Attackers;
using BattleSimulation.World;
using Game.Blueprint;
using UnityEngine;
using Utils;

namespace BattleSimulation.Placement
{
    public class PlacementController : MonoBehaviour
    {
        LayerMask coarseTerrainMask_;
        [SerializeField] TileSelection tileSelection;
        public PlacementState placementState;
        public Placement placing;

        void Awake()
        {
            coarseTerrainMask_ = LayerMask.GetMask(LayerNames.COARSE_TERRAIN);
        }
        void Update()
        {
            placementState.hoveredTile = tileSelection.hoveredTile;

            if (placing == null)
                return;

            if (Input.GetKeyDown(KeyCode.R))
                placementState.rotation++;

            Ray ray = tileSelection.mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100, coarseTerrainMask_))
                placementState.tilePos = WorldUtils.WorldPosToTilePos(hit.point);
        }

        public void Deselect()
        {
            if (placing == null)
                return;
            Destroy(placing.gameObject);
        }

        public void Select(Blueprint blueprint)
        {
            placing = Instantiate(blueprint.prefab, transform).GetComponent<Placement>();
            placing.GetComponent<IBlueprinted>().InitBlueprint(blueprint);
        }

        public bool Place()
        {
            placing.Setup(placementState);
            if (!placing.IsValid())
                return false;
            placing.Place();
            placing = null;
            return true;
        }

        void OnDrawGizmos()
        {
            if (placing == null)
                return;

            placing.Setup(placementState);
            bool valid = placing.IsValid();
            Gizmos.color = valid ? Color.green : Color.red;
            Gizmos.DrawSphere(WorldUtils.TilePosToWorldPos(placementState.tilePos), 0.25f);

            if (!valid)
                return;

            foreach (var t in placing.GetAffectedTiles())
            {
                var pos = t.pos;
                var pos3d = new Vector3(pos.x, pos.y, World.WorldData.World.data.tiles.GetHeightAt(pos) ?? 0);
                Gizmos.DrawWireCube(WorldUtils.TilePosToWorldPos(pos3d), Vector3.one * 0.5f);
            }
            foreach (var a in placing.GetAffectedAttackers())
            {
                Gizmos.DrawWireSphere(a.transform.position, 0.5f);
            }

            var region = placing.GetAffectedRegion();
            for (float y = -1; y < WorldUtils.WORLD_SIZE.y; y += 0.25f)
            {
                for (float x = -1; x < WorldUtils.WORLD_SIZE.x; x += 0.25f)
                {
                    var pos = WorldUtils.TilePosToWorldPos(new Vector3(x, y, World.WorldData.World.data.tiles.GetHeightAt(new(x, y)) ?? 0));
                    Gizmos.color = region(pos + Attacker.LARGE_TARGET_HEIGHT * Vector3.up) ? (region(pos + Attacker.SMALL_TARGET_HEIGHT * Vector3.up) ? Color.green : Color.yellow) : Color.red;
                    Gizmos.DrawWireCube(pos + Attacker.SMALL_TARGET_HEIGHT * Vector3.up, Vector3.one * 0.1f);
                }
            }
        }
    }
}
