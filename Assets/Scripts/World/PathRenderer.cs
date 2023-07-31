using System.Collections.Generic;
using UnityEngine;
using Utils;
using World.WorldData;

namespace World
{
    public class PathRenderer : MonoBehaviour
    {
        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        [Header("References")]
        [SerializeField] WorldData.WorldData worldData;
        [SerializeField] GameObject linePrefab;
        [SerializeField] Material mat;
        [Header("Settings")]
        [SerializeField] float height;
        [SerializeField] float scrollSpeed;
        [SerializeField] float width;
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
            for (int i = 0; i < worldData.firstPathNodes.Length; i++)
            {
                DrawPath(worldData.pathStarts[i], worldData.tiles[worldData.firstPathNodes[i]]);
            }
        }

        void MakeSegment(Vector2Int start, Vector2Int end)
        {
            taken_.Add((start, end));
            LineRenderer lr = Instantiate(linePrefab, transform).GetComponent<LineRenderer>();
            Vector2 off = 0.5f * width * ((Vector2)(end - start)).normalized;
            float endHeight = worldData.tiles.GetHeightAt(end).GetValueOrDefault(0);
            float startHeight = worldData.tiles.GetHeightAt(start).GetValueOrDefault(worldData.tiles.GetHeightAt(0.6f * (Vector2)end + 0.4f * (Vector2)start).GetValueOrDefault(endHeight));
            lr.SetPositions(new[] {
            WorldUtils.TileToWorldPos(start - off) + Vector3.up * (height + startHeight * WorldUtils.HEIGHT_STEP),
            WorldUtils.TileToWorldPos(start + off) + Vector3.up * (height + startHeight * WorldUtils.HEIGHT_STEP),
            WorldUtils.TileToWorldPos(end   - off) + Vector3.up * (height + endHeight * WorldUtils.HEIGHT_STEP)
        });
        }
    }
}