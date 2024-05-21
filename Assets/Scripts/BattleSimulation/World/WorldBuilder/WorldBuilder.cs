using System.Collections;
using System.Diagnostics;
using BattleSimulation.Buildings;
using BattleSimulation.Selection;
using BattleSimulation.World.WorldData;
using BattleVisuals.World;
using Game.Blueprint;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace BattleSimulation.World.WorldBuilder
{
    public class WorldBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] WorldData.WorldData worldData;
        [SerializeField] Transform tileHolder;
        [SerializeField] Transform terrain;
        [SerializeField] PathRenderer pathRenderer;
        [SerializeField] GameObject slotPrefab;
        [SerializeField] GameObject tilePrefab;
        public Blueprint hubBlueprint;
        [Header("Settings")]
        [SerializeField] GameObject[] enableWhenReady;
        [SerializeField] UnityEvent onReady;
        [Header("Runtime variables")]
        [SerializeField] int done;
        [SerializeField] bool ready;
        [SerializeField] Tile hubTile;
        [SerializeField] int millisPerFrame = 12;
        readonly Stopwatch frameTimer_ = new();

        void Awake()
        {
            Tiles.TILES.Fill((Tile)null);
        }

        void Update()
        {
            millisPerFrame = Mathf.Clamp(1000 / Application.targetFrameRate, 10, 100) / 2;
            frameTimer_.Restart();
            if (done >= 5 && !ready)
                Ready();
        }

        void Ready()
        {
            ready = true;
            onReady.Invoke();
            foreach (var o in enableWhenReady)
                o.SetActive(true);
        }

        public void PlaceTilesTrigger() => StartCoroutine(PlaceTiles());
        public void BuildTerrainTrigger() => StartCoroutine(BuildTerrain());
        public void PlaceDecorationsTrigger() => StartCoroutine(PlaceDecorations());
        public void RenderPathTrigger() => StartCoroutine(RenderPath());
        public void PlaceHubTrigger() => StartCoroutine(PlaceHub());

        IEnumerator PlaceTiles()
        {
            foreach (var pos in WorldUtils.WORLD_SIZE)
            {
                PlaceTile(pos);
                if (frameTimer_.ElapsedMilliseconds >= millisPerFrame)
                    yield return null;
            }

            hubTile = Tiles.TILES[worldData.hubPosition];
            done++;
        }

        IEnumerator BuildTerrain()
        {
            foreach (var pos in WorldUtils.WORLD_SIZE + Vector2Int.one)
            {
                PlaceModule(pos);
                if (frameTimer_.ElapsedMilliseconds >= millisPerFrame)
                    yield return null;
            }

            done++;
        }

        IEnumerator PlaceDecorations()
        {
            foreach (var tile in worldData.tiles)
            {
                if (Tiles.TILES[tile.pos] == null)
                    yield return new WaitUntil(() => Tiles.TILES[tile.pos] != null);
                foreach (var decoration in tile.decorations)
                {
                    PlaceDecoration(decoration, Tiles.TILES[tile.pos].decorationHolder);
                    if (frameTimer_.ElapsedMilliseconds >= millisPerFrame)
                        yield return null;
                }
            }

            done++;
        }

        IEnumerator RenderPath()
        {
            pathRenderer.RenderPaths();
            done++;
            yield break;
        }

        void PlaceModule(Vector2Int pos)
        {
            var slot = worldData.terrain[pos];
            Transform t = Instantiate(slotPrefab, terrain).transform;
            t.position = WorldUtils.SlotPosToWorldPos(pos.x, pos.y, slot.height + slot.module.HeightOffset);
            t.localScale = new Vector3(slot.module.Flipped ? -1 : 1, 1, 1) * 1.01f;
            t.localRotation = Quaternion.Euler(0, 90 * slot.module.Rotated, 0);
            t.GetComponent<MeshFilter>().mesh = slot.module.CollisionMesh;
            t.GetComponent<MeshCollider>().sharedMesh = slot.module.CollisionMesh;
        }

        void PlaceDecoration(DecorationInstance decoration, Transform parent)
        {
            Transform t = Instantiate(decoration.decoration.Prefab, parent).transform;
            t.position = WorldUtils.TilePosToWorldPos(decoration.position.x, decoration.position.y, worldData.tiles.GetHeightAt(decoration.position));
            t.localScale = Vector3.one * decoration.size;
            t.localRotation = Quaternion.Euler(decoration.eulerRotation);
        }

        void PlaceTile(Vector2Int pos)
        {
            Tile t = Instantiate(tilePrefab, tileHolder.transform).GetComponent<Tile>();
            t.pos = pos;
            TileData tileData = worldData.tiles[pos];
            t.slant = tileData.slant;
            if (tileData.dist != int.MaxValue)
                t.obstacle = Tile.Obstacle.Path;
            else if (tileData.obstacle is null)
                t.obstacle = Tile.Obstacle.None;
            else
                t.obstacle = (Tile.Obstacle)((int)tileData.obstacle.ObstacleType + 2);
            t.height = worldData.tiles.GetHeightAt(pos);
            t.transform.localPosition = WorldUtils.TilePosToWorldPos(pos.x, pos.y, t.height);
            Tiles.TILES[pos] = t;
        }

        IEnumerator PlaceHub()
        {
            while (hubTile == null)
                yield return null;
            PlacePermanentBuilding(hubBlueprint, hubTile.pos);
            done++;
        }

        public void PlacePermanentBuilding(Blueprint blueprint, Vector2Int tilePos)
        {
            var tile = Tiles.TILES[tilePos];
            var building = Instantiate(blueprint.prefab, transform).GetComponent<Building>();
            building.InitBlueprint(blueprint);
            var placement = building.GetComponent<Placement>();
            placement.Setup(tile.GetComponentInChildren<Selectable>(), 0, null, null);
            placement.Place();
            building.permanent = true;
        }
    }
}