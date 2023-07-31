using Data.WorldGen;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace World.WorldData
{
    public class TilesData
    {
        readonly Array2D<TileData> tiles_;

        public TilesData(IReadOnlyArray2D<(Module module, int height)> slots, Func<Vector2Int, int, bool> isPassable, IEnumerable<Vector2Int[]> paths)
        {
            tiles_ = new(WorldUtils.WORLD_SIZE);
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                tiles_[v] = new();

            foreach (Vector2Int pos in WorldUtils.WORLD_SIZE)
            {
                CardinalDirs<TileData> connections = new();
                for (int d = 0; d < 4; d++)
                {
                    Vector2Int p = pos + WorldUtils.CARDINAL_DIRS[d];
                    if (tiles_.IsInBounds(p) && isPassable(pos, d))
                        connections[d] = this[p];
                    else
                        connections[d] = null;
                }

                TileData t = tiles_[pos];
                t.pos = pos;
                t.height = slots[pos].height + slots[pos].module.Shape.Heights.NE;
                t.slant = slots[pos].module.Shape.Slants.NE;
                t.passable = true;
                t.neighbors = connections;
                t.dist = int.MaxValue;
                t.blocker = null;
            }
            foreach (var path in paths)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    tiles_[path[i]].dist = path.Length - i;
                }
            }
        }

        public TileData this[Vector2Int pos] => tiles_[pos];
        public IEnumerator<TileData> GetEnumerator() => tiles_.GetEnumerator();

        public void RecalculatePaths()
        {
            foreach (var node in tiles_)
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
                    if (n is null || n.dist != int.MaxValue || !n.passable)
                        continue;

                    if (queue.Contains(n.pos))
                    {
                        if (queue.PeekPriority(n.pos) > dist + 1)
                            queue.ChangePriority(n.pos, dist + 1);
                    }
                    else
                    {
                        queue.Enqueue(n.pos, dist + 1);
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
