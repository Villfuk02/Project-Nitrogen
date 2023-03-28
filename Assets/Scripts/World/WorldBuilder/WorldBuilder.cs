using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Blockers;
using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer;
using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Utils;
using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.WFC;
using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using InfiniteCombo.Nitrogen.Assets.Scripts.World;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using static InfiniteCombo.Nitrogen.Assets.Scripts.World.WorldData.WorldData;

namespace Assets.Scripts.World.WorldBuilder
{
    public class WorldBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] World world;
        [SerializeField] Transform tiles;
        [SerializeField] Transform terrain;
        [SerializeField] Transform decorations;
        [SerializeField] PathRenderer pr;
        [Header("Setup")]
        [SerializeField] GameObject slotPrefab;
        [SerializeField] GameObject tilePrefab;
        [Header("Runtime")]
        [SerializeField] int done;
        readonly Stopwatch frameTimer = new();
        const int MILLIS_PER_FRAME = 12;
        public void Start()
        {
            StartCoroutine(PlaceTiles(20));
            StartCoroutine(BuildTerrain(10));
            StartCoroutine(PlaceDecorations(10));
            StartCoroutine(RenderPath());
        }
        private void Update()
        {
            frameTimer.Restart();
            if (done >= 4)
                world.ready = true;
        }

        IEnumerator PlaceTiles(int batchSize)
        {
            while (done < 3)
                yield return null;
            int p = 0;
            for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
            {
                for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
                {
                    PlaceTile(x, y);
                    p++;
                    if (p % batchSize == 0 && frameTimer.ElapsedMilliseconds >= MILLIS_PER_FRAME)
                    {
                        yield return null;
                    }
                }
            }
            done++;
            yield break;
        }
        IEnumerator BuildTerrain(int batchSize)
        {
            while (WORLD_DATA?.modules is null)
                yield return null;
            int p = 0;
            for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
            {
                for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
                {
                    PlaceModule(x, y);
                    p++;
                    if (p % batchSize == 0 && frameTimer.ElapsedMilliseconds >= MILLIS_PER_FRAME)
                    {
                        yield return null;
                    }
                }
            }
            done++;
        }
        IEnumerator PlaceDecorations(int batchSize)
        {
            while (WORLD_DATA?.decorationPositions is null)
                yield return null;
            int p = 0;
            for (int i = 0; i < WORLD_DATA.decorationPositions.Length; i++)
            {
                for (int j = 0; j < WORLD_DATA.decorationPositions[i].Count; j++)
                {
                    PlaceDecoration(i, WORLD_DATA.decorationPositions[i][j], WORLD_DATA.decorationScales[i][j]);
                    p++;
                    if (p % batchSize == 0 && frameTimer.ElapsedMilliseconds >= MILLIS_PER_FRAME)
                    {
                        yield return null;
                    }
                }
            }
            done++;
        }

        IEnumerator RenderPath()
        {
            while (WORLD_DATA?.tiles is null || WORLD_DATA.pathStarts is null)
                yield return null;
            pr.RenderPaths();
            done++;
        }


        void PlaceModule(int x, int y)
        {
            WFCModule m = WFCGenerator.ALL_MODULES[WORLD_DATA.modules[x, y]];
            Transform t = Instantiate(slotPrefab, terrain).transform;
            t.position = WorldUtils.SlotToWorldPos(x, y, WORLD_DATA.moduleHeights[x, y] - m.meshHeightOffset);
            t.localScale = new Vector3(m.flip ? -1 : 1, 1, 1) * 1.01f;
            t.localRotation = Quaternion.Euler(0, 90 * m.rotate, 0);
            t.GetComponent<MeshFilter>().mesh = m.mesh;
            t.GetComponent<MeshCollider>().sharedMesh = m.mesh;
        }
        private void PlaceDecoration(int module, Vector2 pos, float scale)
        {
            ScattererObjectModule m = Scatterer.SCATTERER_MODULES[module];
            Transform t = Instantiate(m.prefab, decorations).transform;
            t.position = WorldUtils.TileToWorldPos(pos.x, pos.y, WORLD_DATA.tiles.GetHeightAt(pos).Value);
            t.localScale = Vector3.one * scale;
            Vector2 r = Random.insideUnitCircle * m.angleSpread;
            t.localRotation = Quaternion.Euler(r.x, Random.Range(0, 360f), r.y);
        }
        void PlaceTile(int x, int y)
        {
            Tile t = Instantiate(tilePrefab, tiles).GetComponent<Tile>();
            t.pos = new(x, y);
            LevelGenTile tt = WORLD_DATA.tiles[t.pos];
            t.slant = tt.slant != WorldUtils.Slant.None;
            if (tt.dist != int.MaxValue)
                t.obstacle = Tile.Obstacle.Path;
            else if (tt.blocker == -1)
                t.obstacle = Tile.Obstacle.None;
            else
                t.obstacle = (Tile.Obstacle)((int)BlockerGenerator.ALL_BLOCKERS[tt.blocker].type + 2);
            t.transform.localPosition = WorldUtils.TileToWorldPos(x, y, WORLD_DATA.tiles.GetHeightAt(new Vector2(x, y)).GetValueOrDefault(-2));
            if (t.slant)
            {
                t.slantedParts.Rotate(WorldUtils.WORLD_CARDINAL_DIRS[((int)tt.slant) % 4] * WorldUtils.SLANT_ANGLE);
            }
        }
    }
}
