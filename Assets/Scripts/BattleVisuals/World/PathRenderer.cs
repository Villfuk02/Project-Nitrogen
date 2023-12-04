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
        [SerializeField] float height;
        [SerializeField] float scrollSpeed;
        [SerializeField] float width;
        [SerializeField] Vector2 labelPos;
        [Header("Runtime Values")]
        [SerializeField] float offset;
        readonly HashSet<(Vector2Int, Vector2Int)> taken_ = new();

        void OnApplicationQuit()
        {
            mat.SetTextureOffset(MainTex, Vector2.zero);
        }

        void Update()
        {
            offset += scrollSpeed * Time.deltaTime;
            mat.SetTextureOffset(MainTex, Vector2.right * offset);
        }

        public void RenderPaths()
        {
            void DrawPath(Vector2Int? from, TileData t)
            {
                if (from is not null)
                {
                    if (!taken_.Contains((from.Value, t.pos)))
                    {
                        MakeSegment(from.Value, t.pos);
                    }
                }
                foreach (TileData nt in t.pathNext)
                {
                    DrawPath(t.pos, nt);
                }
            }
            for (int i = 0; i < worldData.firstPathTiles.Length; i++)
            {
                DrawPath(worldData.pathStarts[i], worldData.tiles[worldData.firstPathTiles[i]]);
                MakeLabel(worldData.pathStarts[i], worldData.firstPathTiles[i], (char)('A' + i));
            }
        }

        void MakeSegment(Vector2Int start, Vector2Int end)
        {
            taken_.Add((start, end));
            LineRenderer lr = Instantiate(linePrefab, transform).GetComponent<LineRenderer>();
            Vector2 off = 0.5f * width * ((Vector2)(end - start)).normalized;
            (float startHeight, float endHeight) = GetSegmentHeights(start, end);
            lr.SetPositions(new[] {
            WorldUtils.TilePosToWorldPos(start - off) + Vector3.up * (height + startHeight * WorldUtils.HEIGHT_STEP),
            WorldUtils.TilePosToWorldPos(start + off) + Vector3.up * (height + startHeight * WorldUtils.HEIGHT_STEP),
            WorldUtils.TilePosToWorldPos(end   - off) + Vector3.up * (height + endHeight * WorldUtils.HEIGHT_STEP)
        });
        }

        void MakeLabel(Vector2Int start, Vector2Int first, char label)
        {
            (float startHeight, float _) = GetSegmentHeights(start, first);
            var lgo = Instantiate(pathLabelPrefab, transform);
            lgo.GetComponentInChildren<TextMeshProUGUI>().text = label.ToString();
            var startPos = WorldUtils.TilePosToWorldPos(start);
            var firstPos = WorldUtils.TilePosToWorldPos(first);
            var offset = startPos - firstPos;
            Vector3 pos = WorldUtils.TilePosToWorldPos(start) + offset * labelPos.x + Vector3.up * (labelPos.y + startHeight * WorldUtils.HEIGHT_STEP);
            lgo.transform.localPosition = pos;
        }

        (float startHeight, float endHeight) GetSegmentHeights(Vector2Int start, Vector2Int end)
        {
            float endHeight = worldData.tiles.GetHeightAt(end).GetValueOrDefault(0);
            float startHeight = worldData.tiles.GetHeightAt(start)
                .GetValueOrDefault(worldData.tiles.GetHeightAt(0.6f * (Vector2)end + 0.4f * (Vector2)start).GetValueOrDefault(endHeight));
            return (startHeight, endHeight);
        }
    }
}