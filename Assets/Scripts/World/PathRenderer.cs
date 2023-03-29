using Assets.Scripts.LevelGen.Utils;
using Assets.Scripts.Utils;
using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.World.WorldData.WorldData;

namespace Assets.Scripts.World
{
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] GameObject linePrefab;
        [SerializeField] float inset;
        [SerializeField] float height;
        [SerializeField] float scrollSpeed;
        [SerializeField] float offset;
        [SerializeField] float width;
        [SerializeField] Material mat;
        readonly HashSet<(Vector2Int, Vector2Int)> taken = new();
        private void OnApplicationQuit()
        {
            mat.SetTextureOffset("_MainTex", Vector2.zero);
        }
        private void Update()
        {
            offset += scrollSpeed * Time.deltaTime;
            mat.SetTextureOffset("_MainTex", Vector2.right * offset);
        }

        public void RenderPaths()
        {
            void DrawPath(Vector2Int? from, LevelGenTile t)
            {
                if (from is not null)
                {
                    if (!taken.Contains((from.Value, t.pos)))
                    {
                        MakeSegment(from.Value, t.pos);
                    }
                }
                foreach (LevelGenTile nt in t.pathNext)
                {
                    DrawPath(t.pos, nt);
                }
            }
            for (int i = 0; i < WORLD_DATA.firstPathNodes.Length; i++)
            {
                DrawPath(WORLD_DATA.pathStarts[i], WORLD_DATA.tiles[WORLD_DATA.firstPathNodes[i]]);
            }
        }

        void MakeSegment(Vector2Int start, Vector2Int end)
        {
            taken.Add((start, end));
            LineRenderer lr = Instantiate(linePrefab, transform).GetComponent<LineRenderer>();
            Vector2 off = 0.5f * width * ((Vector2)(end - start)).normalized;
            float endHeight = WORLD_DATA.tiles.GetHeightAt(end).GetValueOrDefault(0);
            float startHeight = WORLD_DATA.tiles.GetHeightAt(start).GetValueOrDefault(WORLD_DATA.tiles.GetHeightAt(0.6f * (Vector2)end + 0.4f * (Vector2)start).GetValueOrDefault(endHeight));
            lr.SetPositions(new Vector3[] {
            WorldUtils.TileToWorldPos(start - off) + Vector3.up * (height + startHeight * WorldUtils.HEIGHT_STEP),
            WorldUtils.TileToWorldPos(start + off) + Vector3.up * (height + startHeight * WorldUtils.HEIGHT_STEP),
            WorldUtils.TileToWorldPos(end   - off) + Vector3.up * (height + endHeight * WorldUtils.HEIGHT_STEP)
        });
        }
    }
}