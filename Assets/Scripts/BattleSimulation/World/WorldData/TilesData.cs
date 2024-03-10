using Data.WorldGen;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace BattleSimulation.World.WorldData
{
    public class TilesData
    {
        public struct CollapsedSlot
        {
            public Module module;
            public int height;
            public static readonly CollapsedSlot NONE = new() { module = null, height = -1 };
        }

        public delegate bool IsPassable(Vector2Int tile, int direction);

        readonly Array2D<TileData> tiles_;
        /// <summary>
        /// Creates an instance of <see cref="TilesData"/>, pre-filled with some base data.
        /// </summary>
        /// <param name="slots">Generated terrain <see cref="Module"/>s and their heights.</param>
        /// <param name="isPassable">Oracle that accepts a tile position and direction and returns if there is a passage from the tile in the given direction.</param>
        /// <param name="paths">The generated paths, each an array of the tile positions it visits.</param>
        public TilesData(IReadOnlyArray2D<CollapsedSlot> slots, IsPassable isPassable, IEnumerable<Vector2Int[]> paths)
        {
            tiles_ = new(WorldUtils.WORLD_SIZE);
            tiles_.Fill(() => new());
            foreach (Vector2Int pos in WorldUtils.WORLD_SIZE)
                InitTile(slots, isPassable, pos);

            foreach (var path in paths)
                for (int i = 0; i < path.Length; i++)
                    tiles_[path[i]].dist = path.Length - i;
        }

        void InitTile(IReadOnlyArray2D<CollapsedSlot> slots, IsPassable isPassable, Vector2Int pos)
        {
            TileData t = this[pos];
            CardinalDirs<TileData> connections = new();
            for (int d = 0; d < 4; d++)
            {
                Vector2Int p = pos + WorldUtils.CARDINAL_DIRS[d];
                if (tiles_.IsInBounds(p) && isPassable(pos, d))
                    connections[d] = this[p];
            }

            t.pos = pos;
            t.height = slots[pos].height + slots[pos].module.Shape.Heights.NE;
            t.slant = slots[pos].module.Shape.Slants.NE;
            t.passable = true;
            t.neighbors = connections;
            t.dist = int.MaxValue;
            t.obstacle = null;
        }

        /// <summary>
        /// Gets the <see cref="TileData"/> at the given tile position.
        /// </summary>
        public TileData this[Vector2Int pos] => tiles_[pos];
        public IEnumerator<TileData> GetEnumerator() => tiles_.GetEnumerator();

        /// <summary>
        /// Recalculates the distance of each tile to the center, using only valid passages and passable tiles. Unreachable tiles have the distance <see cref="int.MaxValue"/>.
        /// </summary>
        public void RecalculateDistances()
        {
            foreach (var node in tiles_)
                node.dist = int.MaxValue;

            tiles_[WorldUtils.WORLD_CENTER].dist = 0;
            Queue<TileData> queue = new();
            queue.Enqueue(tiles_[WorldUtils.WORLD_CENTER]);

            // BFS
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var n in node.neighbors)
                {
                    if (n is null || n.dist != int.MaxValue || !n.passable)
                        continue;

                    n.dist = node.dist + 1;
                    queue.Enqueue(n);
                }
            }
        }

        /// <summary>
        /// Returns the tile-space height at the given tile-space position, or at the closest position in bounds for points out of bounds.
        /// </summary>
        public float GetHeightAt(Vector2 pos)
        {
            const float border = 0.4999f;
            pos = new(
                Mathf.Clamp(pos.x, -border, WorldUtils.WORLD_SIZE.x - 1 + border),
                Mathf.Clamp(pos.y, -border, WorldUtils.WORLD_SIZE.y - 1 + border)
                );
            Vector2Int tilePos = pos.Round();
            return this[tilePos].GetHeight(pos - tilePos);
        }
    }
}
