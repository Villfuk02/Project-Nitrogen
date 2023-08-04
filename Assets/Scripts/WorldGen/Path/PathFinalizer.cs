using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using World.WorldData;
using static WorldGen.WorldGenerator;

namespace WorldGen.Path
{
    public class PathFinalizer : MonoBehaviour
    {
        int extraPaths_;
        Array2D<float> repulsion_;
        Array2D<bool> hasPath_;
        Array2D<bool> outlined_;
        /// <summary>
        /// Makes the final paths, taking into account terrain and blockers. Then stores them directly in <see cref="Tiles"/>.
        /// </summary>
        /// <param name="starts">Start tile of each path.</param>
        /// <param name="maxExtraPaths">Maximum number of extra branches.</param>
        public void FinalizePaths(Vector2Int[] starts, int maxExtraPaths)
        {
            WaitForStep(StepType.Phase);
            Debug.Log("Finalizing Paths");

            RegisterGizmos(StepType.Phase, () =>
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach (var tile in Tiles)
                {
                    Vector3 pos = WorldUtils.TilePosToWorldPos(tile.pos);
                    if (!tile.passable)
                        gizmos.Add(new GizmoManager.Cube(Color.red, pos, new Vector3(0.6f, 0.1f, 0.6f)));
                    for (int i = 0; i < 4; i++)
                    {
                        if (tile.neighbors[i] is null)
                            gizmos.Add(new GizmoManager.Cube(Color.red, (WorldUtils.TilePosToWorldPos(tile.pos + WorldUtils.CARDINAL_DIRS[i]) + pos) * 0.5f, new Vector3(0.4f, 0.1f, 0.4f)));
                    }
                }

                return gizmos;
            });

            extraPaths_ = maxExtraPaths;
            int targetCount = starts.Length;
            var paths = new TileData[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                paths[i] = Tiles[starts[i]];
            }

            repulsion_ = new(WorldUtils.WORLD_SIZE);
            outlined_ = new(WorldUtils.WORLD_SIZE);
            hasPath_ = new(WorldUtils.WORLD_SIZE) { [WorldUtils.WORLD_CENTER] = true };
            UpdateRepulsionField(WorldUtils.WORLD_CENTER);

            for (int i = 0; i < targetCount; i++)
            {
                //the method below doesn't differentiate between the first path and side branches, so we increment extraPaths to account for the first path
                extraPaths_++;
                TracePath(paths[i]);
            }

            foreach (var tile in Tiles)
            {
                if (!hasPath_[tile.pos])
                    tile.dist = int.MaxValue;
            }
        }

        /// <summary>
        /// Draw paths from one start tile.
        /// </summary>
        void TracePath(TileData start)
        {
            RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(start.pos), 0.3f));
            bool foundAny = false;
            LinkedList<(TileData tile, Vector2Int[] path)> stack = new();
            stack.AddFirst((start, new[] { start.pos }));
            bool lastFound = false;
            //basically DFS, but once a path is finalized, take the next step from the bottom of the stack
            while (extraPaths_ > 0 && stack.Count > 0)
            {
                WaitForStep(StepType.MicroStep);
                (TileData currentTile, var path) = lastFound ? stack.First.Value : stack.Last.Value;
                if (lastFound)
                    stack.RemoveFirst();
                else
                    stack.RemoveLast();
                lastFound = PathStep(currentTile, path);
                if (lastFound)
                    foundAny = true;
            }
            WaitForStep(StepType.MicroStep);

            //adds all valid one-tile continuations to the stack, ordered by how good they are 
            bool PathStep(TileData tile, Vector2Int[] path)
            {
                RegisterGizmos(StepType.MicroStep, () =>
                {
                    List<GizmoManager.GizmoObject> gizmos = new()
                    {
                            new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(tile.pos), 0.2f)
                    };
                    TileData prev = null;
                    foreach (var pos in path)
                    {
                        TileData current = Tiles[pos];
                        if (prev is not null)
                        {
                            gizmos.Add(new GizmoManager.Line(Color.magenta, WorldUtils.TilePosToWorldPos(pos), WorldUtils.TilePosToWorldPos(prev.pos)));
                        }

                        prev = current;
                    }

                    return gizmos;
                });

                //if this tile already has a path, check if the path can join to it
                if (hasPath_[tile.pos])
                {
                    //reject the branch if it doesn't do through a tile that's at least two tiles from any other path
                    if (foundAny && path.All(p => outlined_[p]))
                        return false;

                    //otherwise accept it
                    TileData prev = null;
                    foreach (var pos in path)
                    {
                        hasPath_[pos] = true;
                        foreach (var dir in WorldUtils.ADJACENT_DIRS)
                            outlined_.TrySet(pos + dir, true);
                        UpdateRepulsionField(pos);

                        TileData current = Tiles[pos];
                        if (prev is not null)
                        {
                            if (!prev.pathNext.Contains(current))
                                prev.pathNext.Add(current);
                            RegisterGizmos(StepType.Phase, () => new GizmoManager.Line(Color.cyan, WorldUtils.TilePosToWorldPos(pos), WorldUtils.TilePosToWorldPos(prev.pos)));
                        }

                        prev = current;
                    }
                    extraPaths_--;

                    WaitForStep(StepType.Step);
                    RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(start.pos), 0.3f));
                    RegisterGizmos(StepType.Step, () => outlined_.IndexedEnumerable.Where(p => !p.value).Select(p => new GizmoManager.Cube(Color.green, WorldUtils.TilePosToWorldPos(p.index), 0.15f)));
                    return true;
                }

                //find all valid continuations
                var validNeighbors = new List<TileData>();
                for (int i = 0; i < 4; i++)
                {
                    //the next tile must be one closer to the center
                    if (tile.neighbors[i] is not TileData neighbor || neighbor.dist != tile.dist - 1)
                        continue;
                    validNeighbors.Add(neighbor);
                }

                if (validNeighbors.Count == 0)
                    return false;

                //order them by ascending repulsion (trying to avoid other paths), but put the option that goes straight first
                //however, order is reversed, because paths are added like to a stack
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

                //add them to the stack
                foreach (var neighbor in validNeighbors)
                {
                    var newPath = new Vector2Int[path.Length + 1];
                    Array.Copy(path, newPath, path.Length);
                    newPath[^1] = neighbor.pos;
                    stack.AddLast((neighbor, newPath));
                }
                return false;
            }
        }

        /// <summary>
        /// Adds values to the repulsion field, bigger values closer to the pathNode provided.
        /// </summary>
        void UpdateRepulsionField(Vector2Int pathNode)
        {
            foreach (var pos in WorldUtils.WORLD_SIZE)
            {
                repulsion_[pos] += 1f / ((pos - pathNode).sqrMagnitude + 1);
            }
        }
    }
}
