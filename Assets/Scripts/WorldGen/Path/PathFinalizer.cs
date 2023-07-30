using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using WorldGen.Utils;
using static WorldGen.WorldGenerator;

namespace WorldGen.Path
{
    public class PathFinalizer : MonoBehaviour
    {
        int extraPaths_;
        Array2D<float> repulsion_;
        Array2D<bool> hasPath_;
        Array2D<bool> outlined_;
        public void FinalizePaths(Vector2Int[] starts, int maxExtraPaths)
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
                        gizmos.Add(new GizmoManager.Cube(Color.red, pos, new Vector3(0.6f, 0.1f, 0.6f)));
                    for (int i = 0; i < 4; i++)
                    {
                        if (tile.neighbors[i] is null)
                            gizmos.Add(new GizmoManager.Cube(Color.red, (WorldUtils.TileToWorldPos(tile.pos + WorldUtils.CARDINAL_DIRS[i]) + pos) * 0.5f, new Vector3(0.4f, 0.1f, 0.4f)));
                    }
                }

                return gizmos;
            });

            extraPaths_ = maxExtraPaths;
            int targetCount = starts.Length;
            var paths = new WorldGenTile[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                paths[i] = Tiles[starts[i]];
            }

            repulsion_ = new(WorldUtils.WORLD_SIZE);
            outlined_ = new(WorldUtils.WORLD_SIZE);
            hasPath_ = new(WorldUtils.WORLD_SIZE) { [WorldUtils.ORIGIN] = true };
            UpdateRepulsionField(WorldUtils.ORIGIN);

            for (int i = 0; i < targetCount; i++)
            {
                extraPaths_++;
                TracePathQueued(paths[i]);
            }

            foreach (var tile in Tiles)
            {
                if (!hasPath_[tile.pos])
                    tile.dist = int.MaxValue;
            }
        }

        void TracePathQueued(WorldGenTile start)
        {
            RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(start.pos), 0.3f));
            bool foundAny = false;
            LinkedList<(WorldGenTile tile, Vector2Int[] path)> queue = new();
            queue.AddFirst((start, new[] { start.pos }));
            bool lastFound = false;
            while (extraPaths_ > 0 && queue.Count > 0)
            {
                WaitForStep(StepType.MicroStep);
                (WorldGenTile currentTile, var path) = lastFound ? queue.First.Value : queue.Last.Value;
                if (lastFound)
                    queue.RemoveFirst();
                else
                    queue.RemoveLast();
                lastFound = TracePath(currentTile, path);
                if (lastFound)
                    foundAny = true;
            }
            WaitForStep(StepType.MicroStep);

            bool TracePath(WorldGenTile tile, Vector2Int[] path)
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

                if (hasPath_[tile.pos])
                {
                    if (foundAny && path.All(p => outlined_[p]))
                        return false;

                    WorldGenTile prev = null;
                    foreach (var pos in path)
                    {
                        hasPath_[pos] = true;
                        foreach (var dir in WorldUtils.ADJACENT_DIRS)
                            outlined_.TrySet(pos + dir, true);
                        UpdateRepulsionField(pos);

                        WorldGenTile current = Tiles[pos];
                        if (prev is not null)
                        {
                            if (!prev.pathNext.Contains(current))
                                prev.pathNext.Add(current);
                            RegisterGizmos(StepType.Phase, () => new GizmoManager.Line(Color.cyan, WorldUtils.TileToWorldPos(pos), WorldUtils.TileToWorldPos(prev.pos)));
                        }

                        prev = current;
                    }
                    extraPaths_--;

                    WaitForStep(StepType.Step);
                    RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(start.pos), 0.3f));
                    RegisterGizmos(StepType.Step, () => outlined_.IndexedEnumerable.Where(p => !p.value).Select(p => new GizmoManager.Cube(Color.green, WorldUtils.TileToWorldPos(p.index), 0.15f)));
                    return true;
                }

                var validNeighbors = new List<WorldGenTile>();
                for (int i = 0; i < 4; i++)
                {
                    if (tile.neighbors[i] is not WorldGenTile neighbor || neighbor.dist != tile.dist - 1)
                        continue;
                    validNeighbors.Add(neighbor);
                }
                if (validNeighbors.Count == 0)
                    return false;

                // order is reversed, because paths are added like to a stack
                validNeighbors = validNeighbors.OrderByDescending(n => repulsion_[n.pos]).ToList();
                if (path.Length > 1)
                {
                    var straight = validNeighbors.Find(n => n.pos.x == path[^2].x || n.pos.y == path[^2].y);
                    if (straight != null)
                    {
                        validNeighbors.Remove(straight);
                        validNeighbors.Add(straight);
                    }
                }

                foreach (var neighbor in validNeighbors)
                {
                    var newPath = new Vector2Int[path.Length + 1];
                    Array.Copy(path, newPath, path.Length);
                    newPath[^1] = neighbor.pos;
                    queue.AddLast((neighbor, newPath));
                }
                return false;
            }
        }

        void UpdateRepulsionField(Vector2Int path)
        {
            foreach (var pos in WorldUtils.WORLD_SIZE)
            {
                repulsion_[pos] += 1f / ((pos - path).sqrMagnitude + 1);
            }
        }
    }
}
