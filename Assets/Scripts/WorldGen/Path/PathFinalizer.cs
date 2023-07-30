using Random;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using WorldGen.Utils;
using static WorldGen.WorldGenerator;

namespace WorldGen.Path
{
    public class PathFinalizer : MonoBehaviour
    {
        [SerializeField] int maxPathsPerTarget;
        [SerializeField] int minMergeDistance;
        public void FinalizePaths(Vector2Int[] starts)
        {
            WaitForStep(StepType.Phase);
            Debug.Log("Finalizing Paths");

            RegisterGizmos(StepType.Phase, () =>
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach (var tile in Tiles)
                {
                    Vector3 pos = WorldUtils.TileToWorldPos(tile.pos);
                    if (!tile.passable)
                        gizmos.Add(new GizmoManager.Cube(Color.red, pos, new Vector3(0.4f, 0.1f, 0.4f)));
                    for (int i = 0; i < 4; i++)
                    {
                        if (tile.neighbors[i] is null)
                            gizmos.Add(new GizmoManager.Cube(Color.red, (WorldUtils.TileToWorldPos(tile.pos + WorldUtils.CARDINAL_DIRS[i]) + pos) * 0.5f, new Vector3(0.25f, 0.1f, 0.25f)));
                    }
                }

                return gizmos;
            });

            int targetCount = starts.Length;
            Array2D<bool> pathTiles = new(WorldUtils.WORLD_SIZE);
            var paths = new WorldGenTile[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                paths[i] = Tiles[starts[i]];
                pathTiles[paths[i].pos.x, paths[i].pos.y] = true;
            }

            for (int i = 0; i < targetCount; i++)
            {
                WaitForStep(StepType.Step);
                TracePathQueued(paths[i], maxPathsPerTarget);
            }

            WaitForStep(StepType.Step);
            foreach (var tile in Tiles)
            {
                if (tile.pathNext.Count == 0 && tile.pos != WorldUtils.ORIGIN)
                    tile.dist = int.MaxValue;
            }
        }

        void TracePathQueued(WorldGenTile t, int pathsLeft)
        {
            RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(t.pos), 0.3f));
            HashSet<Vector2Int> taken = new();
            LinkedList<(WorldGenTile t, Vector2Int[] path, int distToMerge)> queue = new();
            queue.AddFirst((t, new[] { t.pos }, 0));
            bool lastFound = false;
            while (pathsLeft > 0 && queue.Count > 0)
            {
                WaitForStep(StepType.MicroStep);
                (WorldGenTile u, var path, int distToMerge) = lastFound ? queue.First.Value : queue.Last.Value;
                if (lastFound)
                    queue.RemoveFirst();
                else
                    queue.RemoveLast();
                lastFound = TracePath(u, path, distToMerge);
            }

            bool TracePath(WorldGenTile tile, Vector2Int[] path, int distToMerge)
            {
                RegisterGizmos(StepType.MicroStep, () =>
                {
                    List<GizmoManager.GizmoObject> gizmos = new()
                    {
                            new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(tile.pos), 0.2f)
                    };
                    WorldGenTile prev = null;
                    foreach (var pos in path)
                    {
                        WorldGenTile current = Tiles[pos];
                        if (prev is not null)
                        {
                            gizmos.Add(new GizmoManager.Line(Color.magenta, WorldUtils.TileToWorldPos(pos), WorldUtils.TileToWorldPos(prev.pos)));
                        }

                        prev = current;
                    }

                    return gizmos;
                });
                if (tile.dist == 0 || (taken.Contains(tile.pos) && distToMerge <= 0))
                {
                    if (pathsLeft <= 0)
                        return true;

                    pathsLeft--;
                    WorldGenTile prev = null;
                    foreach (var pos in path)
                    {
                        taken.Add(pos);
                        WorldGenTile current = Tiles[pos];
                        if (prev is not null)
                        {
                            if (!prev.pathNext.Contains(current))
                                prev.pathNext.Add(current);
                            RegisterGizmos(StepType.Phase, () => new GizmoManager.Line(Color.cyan, WorldUtils.TileToWorldPos(pos), WorldUtils.TileToWorldPos(prev.pos)));
                        }

                        prev = current;
                    }
                    return true;
                }

                distToMerge--;
                int count = 0;
                RandomSet<int> order = new(WorldGenerator.Random.NewSeed());
                for (int i = 0; i < 4; i++)
                {
                    if (tile.neighbors[i] is not WorldGenTile neighbor || neighbor.dist != tile.dist - 1)
                        continue;

                    if (distToMerge > 0 && taken.Contains(neighbor.pos))
                        continue;

                    count++;
                    order.Add(i);
                }

                if (count == 0)
                    return false;

                if (count > 1)
                    distToMerge = minMergeDistance;

                while (order.Count > 0)
                {
                    int c = order.PopRandom();
                    WorldGenTile u = tile.neighbors[c];
                    var newPath = new Vector2Int[path.Length + 1];
                    for (int i = 0; i < path.Length; i++)
                    {
                        newPath[i] = path[i];
                    }

                    newPath[^1] = u.pos;
                    queue.AddLast((u, newPath, distToMerge));
                }

                return false;

            }
        }
    }
}
