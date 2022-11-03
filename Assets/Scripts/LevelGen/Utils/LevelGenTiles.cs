using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.WFC;
using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Utils
{
    public class LevelGenTiles
    {
        readonly LevelGenTile[,] _graph;

        public LevelGenTiles(int[] modules, int[] heights, int[] distances)
        {
            _graph = new LevelGenTile[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                _graph[v.x, v.y] = new();

            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
            {
                int index = (v.x + 1) + v.y * (WorldUtils.WORLD_SIZE.x + 1);
                WFCModule m = WFCGenerator.ALL_MODULES[modules[index]];
                LevelGenTile[] connections = new LevelGenTile[4];
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int p = v + WorldUtils.CARDINAL_DIRS[i];
                    Vector2Int pp = i / 2 == 0 ? v + Vector2Int.one : v;
                    if (p.x >= 0 && p.y >= 0 && p.x < WorldUtils.WORLD_SIZE.x && p.y < WorldUtils.WORLD_SIZE.y
                        && WFCGenerator.ALL_MODULES[modules[pp.x + pp.y * (WorldUtils.WORLD_SIZE.x + 1)]].passable[3 - i])
                        connections[i] = this[p];
                    else
                        connections[i] = null;
                }
                LevelGenTile t = _graph[v.x, v.y];
                t.pos = v;
                t.height = heights[index];
                t.slant = m.slants[0];
                t.passable = true;
                t.neighbors = connections;
                t.dist = distances[v.x + v.y * WorldUtils.WORLD_SIZE.x];
                t.blocker = -1;
            }
        }

        public LevelGenTile this[Vector2Int pos] => _graph[pos.x, pos.y];

        public IEnumerator<LevelGenTile> GetEnumerator()
        {
            foreach (var item in _graph)
            {
                yield return item;
            }
        }

        public void RecalculatePaths()
        {
            foreach (var node in _graph)
            {
                node.dist = int.MaxValue;
            }
            PriorityQueue<Vector2Int, int> queue = new();
            queue.Enqueue(WorldUtils.ORIGIN, 0);
            while (queue.Count > 0)
            {
                queue.TryDequeue(out Vector2Int pos, out int dist);
                this[pos].dist = dist;
                foreach (var n in this[pos].neighbors)
                {
                    if (n != null)
                    {
                        if (n.dist == int.MaxValue && n.passable)
                        {
                            if (queue.Contains(n.pos))
                            {
                                if (queue.PeekPriority(n.pos) > dist + 1)
                                {
                                    queue.ChangePriority(n.pos, dist + 1);
                                }
                            }
                            else
                            {
                                queue.Enqueue(n.pos, dist + 1);
                            }
                        }
                    }
                }
            }
        }

        public float? GetHeightAt(Vector2 pos)
        {
            Vector2Int tilePos = new(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
            if (tilePos.x < 0 || tilePos.y < 0 || tilePos.x >= WorldUtils.WORLD_SIZE.x || tilePos.y >= WorldUtils.WORLD_SIZE.y)
                return null;
            return this[tilePos].GetHeight(pos - tilePos);
        }
    }
}
