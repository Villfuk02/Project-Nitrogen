using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer;
using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.WFC;
using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using static InfiniteCombo.Nitrogen.Assets.Scripts.World.World;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen
{
    public class WorldBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform tiles;
        [SerializeField] Transform terrain;
        [SerializeField] Transform decorations;
        [Header("Setup")]
        [SerializeField] GameObject slotPrefab;
        [SerializeField] Gradient terrainGradient;
        [Header("Runtime")]
        [SerializeField] int done;
        readonly Stopwatch frameTimer = new();
        const int MILLIS_PER_FRAME = 12;
        public void Begin()
        {
            StartCoroutine(PlaceTiles(1));
            StartCoroutine(BuildTerrain(1));
            StartCoroutine(PlaceDecorations(1));
        }
        private void Update()
        {
            frameTimer.Restart();
        }

        IEnumerator PlaceTiles(int batchSize)
        {
            done++;
            yield break;
        }
        IEnumerator BuildTerrain(int batchSize)
        {
            while (WORLD_DATA == null || WORLD_DATA.modules == null)
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
            while (WORLD_DATA == null || WORLD_DATA.decorationPositions == null)
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


        void PlaceModule(int x, int y)
        {
            WFCModule m = WFCGenerator.ALL_MODULES[WORLD_DATA.modules[x, y]];
            Transform t = Instantiate(slotPrefab, terrain).transform;
            t.position = WorldUtils.SlotToWorldPos(x, y, WORLD_DATA.moduleHeights[x, y] - m.meshHeightOffset);
            t.localScale = new Vector3(m.flip ? -1 : 1, 1, 1) * 1.01f;
            t.localRotation = Quaternion.Euler(0, 90 * m.rotate, 0);
            t.GetComponent<MeshRenderer>().material.color = terrainGradient.Evaluate(t.localPosition.y * 0.35f + Random.value * 0.2f);
            t.GetComponent<MeshFilter>().mesh = m.mesh;
            t.GetComponent<MeshCollider>().sharedMesh = m.mesh;
        }
        private void PlaceDecoration(int module, Vector2 pos, float scale)
        {
            ScattererObjectModule m = Scatterer.Scatterer.SCATTERER_MODULES[module];
            Transform t = Instantiate(m.prefab, decorations).transform;
            t.position = WorldUtils.TileToWorldPos(pos.x, pos.y, WORLD_DATA.tiles.GetHeightAt(pos).Value);
            t.localScale = Vector3.one * scale;
            Vector2 r = Random.insideUnitCircle * m.angleSpread;
            t.localRotation = Quaternion.Euler(r.x, Random.Range(0, 360f), r.y);
        }
    }
}
