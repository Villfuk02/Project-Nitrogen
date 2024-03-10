using BattleSimulation.World.WorldData;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Utils;

namespace BattleVisuals.World
{
    public class PathRenderer : MonoBehaviour
    {
        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        [Header("References")]
        [SerializeField] WorldData worldData;
        [SerializeField] GameObject linePrefab;
        [SerializeField] Material mat;
        [SerializeField] GameObject pathLabelPrefab;
        [Header("Settings")]
        [SerializeField] float heightAboveGround;
        [SerializeField] float scrollSpeed;
        [SerializeField] float lineWidth;
        [SerializeField] Vector2 labelPos;
        [Header("Runtime variables")]
        [SerializeField] float materialScrollOffset;
        readonly HashSet<(Vector2Int, Vector2Int)> instantiatedSegments_ = new();

        void OnApplicationQuit()
        {
            mat.SetTextureOffset(MainTex, Vector2.zero);
        }

        void Update()
        {
            materialScrollOffset += scrollSpeed * Time.deltaTime;
            mat.SetTextureOffset(MainTex, Vector2.right * materialScrollOffset);
        }

        public void RenderPaths()
        {
            for (int i = 0; i < worldData.firstPathTiles.Length; i++)
            {
                DrawPathRecursive(worldData.pathStarts[i], worldData.tiles[worldData.firstPathTiles[i]]);
                MakeLabel(worldData.pathStarts[i], worldData.firstPathTiles[i], (char)('A' + i));
            }
        }

        void DrawPathRecursive(Vector2Int? from, TileData t)
        {
            if (from is not null)
                TrySpawnSegment(from.Value, t.pos);

            foreach (TileData nt in t.pathNext)
                DrawPathRecursive(t.pos, nt);
        }

        void TrySpawnSegment(Vector2Int from, Vector2Int to)
        {
            if (instantiatedSegments_.Contains((from, to)))
                return;
            instantiatedSegments_.Add((from, to));

            LineRenderer lr = Instantiate(linePrefab, transform).GetComponent<LineRenderer>();
            Vector2 offset = 0.5f * lineWidth * (Vector2)(to - from);
            float startHeight = worldData.tiles.GetHeightAt(from);
            float endHeight = worldData.tiles.GetHeightAt(to);
            lr.SetPositions(new[] {
                GetPointWorldPos(from - offset, startHeight),
                GetPointWorldPos(from + offset, startHeight),
                GetPointWorldPos(to - offset, endHeight)
            });
        }

        Vector3 GetPointWorldPos(Vector2 tilePos, float heightOffset)
        {
            return WorldUtils.TilePosToWorldPos(tilePos) + Vector3.up * (heightAboveGround + heightOffset * WorldUtils.HEIGHT_STEP);
        }

        void MakeLabel(Vector2Int start, Vector2Int first, char label)
        {
            float startHeight = worldData.tiles.GetHeightAt(start);
            var labelGameObject = Instantiate(pathLabelPrefab, transform);
            labelGameObject.GetComponentInChildren<TextMeshProUGUI>().text = label.ToString();
            var startPos = WorldUtils.TilePosToWorldPos(start);
            var firstPos = WorldUtils.TilePosToWorldPos(first);
            var offset = startPos - firstPos;
            Vector3 pos = WorldUtils.TilePosToWorldPos(new Vector3(start.x, start.y, startHeight)) + offset * labelPos.x + Vector3.up * labelPos.y;
            labelGameObject.transform.localPosition = pos;
        }
    }
}