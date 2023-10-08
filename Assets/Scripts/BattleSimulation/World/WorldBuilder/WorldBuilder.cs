using BattleSimulation.World.WorldData;
using BattleVisuals.World;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace BattleSimulation.World.WorldBuilder
{
    public class WorldBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] WorldData.WorldData worldData;
        [SerializeField] WorldData.World world;
        [SerializeField] Transform tileHolder;
        [SerializeField] Transform terrain;
        [SerializeField] PathRenderer pathRenderer;
        [SerializeField] GameObject[] enableWhenReady;
        [Header("Setup")]
        [SerializeField] GameObject slotPrefab;
        [SerializeField] GameObject tilePrefab;
        [Header("Runtime")]
        [SerializeField] int done;
        readonly Stopwatch frameTimer_ = new();
        const int MILLIS_PER_FRAME = 12; //TODO: Make framerate-based

        void Awake()
        {
            foreach (var (index, _) in Tiles.TILES.IndexedEnumerable)
            {
                Tiles.TILES[index] = null;
            }
        }

        void Update()
        {
            frameTimer_.Restart();
            if (done < 4)
                return;

            world.SetReady();
            foreach (var o in enableWhenReady)
            {
                o.SetActive(true);
            }
        }

        public void PlaceTilesTrigger() => StartCoroutine(PlaceTiles(10));
        public void BuildTerrainTrigger() => StartCoroutine(BuildTerrain(10));
        public void PlaceDecorationsTrigger() => StartCoroutine(PlaceDecorations(10));
        public void RenderPathTrigger() => StartCoroutine(RenderPath());

        IEnumerator PlaceTiles(int batchSize)
        {
            int p = 0;
            foreach (var pos in WorldUtils.WORLD_SIZE)
            {
                PlaceTile(pos);
                p++;
                if (p % batchSize == 0 && frameTimer_.ElapsedMilliseconds >= MILLIS_PER_FRAME)
                {
                    yield return null;
                }
            }
            done++;
        }
        IEnumerator BuildTerrain(int batchSize)
        {
            int p = 0;
            foreach (var pos in WorldUtils.WORLD_SIZE + Vector2Int.one)
            {
                PlaceModule(pos);
                p++;
                if (p % batchSize == 0 && frameTimer_.ElapsedMilliseconds >= MILLIS_PER_FRAME)
                {
                    yield return null;
                }
            }
            done++;
        }
        IEnumerator PlaceDecorations(int batchSize)
        {
            int p = 0;
            foreach (var tile in worldData.tiles)
            {
                if (Tiles.TILES[tile.pos] == null)
                    yield return new WaitUntil(() => Tiles.TILES[tile.pos] != null);
                foreach (var decoration in tile.decorations)
                {
                    PlaceDecoration(decoration, Tiles.TILES[tile.pos].decorationHolder);
                    p++;
                    if (p % batchSize == 0 && frameTimer_.ElapsedMilliseconds >= MILLIS_PER_FRAME)
                    {
                        yield return null;
                    }
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
            (var module, int height) = worldData.terrain[pos];
            Transform t = Instantiate(slotPrefab, terrain).transform;
            t.position = WorldUtils.SlotPosToWorldPos(pos.x, pos.y, height + module.HeightOffset);
            t.localScale = new Vector3(module.Flipped ? -1 : 1, 1, 1) * 1.01f;
            t.localRotation = Quaternion.Euler(0, 90 * module.Rotated, 0);
            t.GetComponent<MeshFilter>().mesh = module.Collision; //TODO: Choose a real model
            t.GetComponent<MeshCollider>().sharedMesh = module.Collision;
        }
        void PlaceDecoration(DecorationInstance decoration, Transform parent)
        {
            Transform t = Instantiate(decoration.decoration.Prefab, parent).transform;
            t.position = WorldUtils.TilePosToWorldPos(decoration.position.x, decoration.position.y, worldData.tiles.GetHeightAt(decoration.position)!.Value);
            t.localScale = Vector3.one * decoration.size;
            Vector2 r = Random.insideUnitCircle * decoration.decoration.AngleSpread;
            t.localRotation = Quaternion.Euler(r.x, Random.Range(0, 360f), r.y);
        }
        void PlaceTile(Vector2Int pos)
        {
            Tile t = Instantiate(tilePrefab, tileHolder.transform).GetComponent<Tile>();
            t.pos = pos;
            TileData tileData = worldData.tiles[pos];
            t.slant = tileData.slant;
            if (tileData.dist != int.MaxValue)
                t.obstacle = Tile.Obstacle.Path;
            else if (tileData.blocker is null)
                t.obstacle = Tile.Obstacle.None;
            else
                t.obstacle = (Tile.Obstacle)((int)tileData.blocker.BlockerType + 2);
            t.transform.localPosition = WorldUtils.TilePosToWorldPos(pos.x, pos.y, worldData.tiles.GetHeightAt(pos)!.Value);
            Tiles.TILES[pos] = t;
        }
    }
}
