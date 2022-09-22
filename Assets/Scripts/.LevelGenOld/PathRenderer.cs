using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.World
{
    public class PathRenderer : MonoBehaviour
    {
        public GameObject linePrefab;
        public float inset;
        public float height;
        public float scrollSpeed;
        public float offset;
        public float width;
        public Material mat;
        bool done = false;
        readonly HashSet<(Vector2, Vector2)> taken = new();
        private void OnApplicationQuit()
        {
            mat.SetTextureOffset("_MainTex", Vector2.zero);
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P) && !done)
            {
                RenderPaths();
                done = true;
            }
            offset += scrollSpeed * Time.deltaTime;
            mat.SetTextureOffset("_MainTex", Vector2.right * offset);
        }

        void RenderPaths()
        {
            void DrawPath(Vector2? from, PathNode n)
            {
                if (from != null)
                {
                    if (!taken.Contains((from.Value, (Vector2)n.pos)))
                    {
                        MakeSegment(from.Value, n.pos);
                    }
                }
                foreach (var item in n.next)
                {
                    DrawPath(n.pos, item);
                }
            }
            for (int i = 0; i < paths.Length; i++)
            {
                DrawPath(null, paths[i]);
            }
        }

        void MakeSegment(Vector2 start, Vector2 end)
        {
            taken.Add((start, end));
            LineRenderer lr = Instantiate(linePrefab, transform).GetComponent<LineRenderer>();
            Vector2 off = 0.5f * width * (end - start).normalized;
            lr.SetPositions(new Vector3[] {
            WorldUtils.TileToWorldPos(start - off) + Vector3.up * (height + WorldUtils.TileToWorldSurface(start).y),
            WorldUtils.TileToWorldPos(start + off) + Vector3.up * (height + WorldUtils.TileToWorldSurface(start).y),
            WorldUtils.TileToWorldPos(end   - off) + Vector3.up * (height + WorldUtils.TileToWorldSurface(end  ).y)
        });
        }
    }
}