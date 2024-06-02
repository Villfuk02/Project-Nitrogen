using System;
using System.Collections.Generic;
using System.Linq;
using BattleSimulation.World.WorldData;
using UnityEngine;
using Utils;
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
        /// Makes the final paths, taking into account terrain and obstacles. Then stores them directly in <see cref="Tiles"/>.
        /// </summary>
        public void FinalizePaths(Vector2Int[] starts, int maxExtraBranches, Vector2Int hubPosition)
        {
            // debug
            WaitForStep(StepType.Phase);
            print("Finalizing Paths");
            // draw all blocked tiles and edges
            RegisterGizmos(StepType.Phase, () =>
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach (var tile in Tiles)
                {
                    Vector3 pos = WorldUtils.TilePosToWorldPos(tile.pos);
                    if (tile.blocked)
                        gizmos.Add(new GizmoManager.Cube(Color.red, pos, new Vector3(0.6f, 0.1f, 0.6f)));
                    for (int i = 0; i < 4; i++)
                    {
                        if (tile.neighbors[i] is null)
                            gizmos.Add(new GizmoManager.Cube(Color.red, (WorldUtils.TilePosToWorldPos(tile.pos + WorldUtils.CARDINAL_DIRS[i]) + pos) * 0.5f, new Vector3(0.4f, 0.1f, 0.4f)));
                    }
                }

                return gizmos;
            });
            // end debug

            int maxDist = Tiles.CalculateMinDistances(hubPosition);

            // debug
            // draw the calculated distances as a gradient
            RegisterGizmos(StepType.Phase, () =>
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach (var tile in Tiles)
                {
                    if (tile.dist == int.MaxValue)
                        continue;
                    Vector3 pos = WorldUtils.TilePosToWorldPos(tile.pos);
                    Color c = Color.Lerp(Color.blue, Color.green, tile.dist / (float)maxDist);
                    gizmos.Add(new GizmoManager.Cube(c, pos, new Vector3(0.3f, 0.3f, 0.3f)));
                }

                return gizmos;
            });
            // end debug

            extraPaths_ = maxExtraBranches;
            int targetCount = starts.Length;
            var paths = new TileData[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                paths[i] = Tiles[starts[i]];
            }

            repulsion_ = new(WorldUtils.WORLD_SIZE);
            outlined_ = new(WorldUtils.WORLD_SIZE);
            hasPath_ = new(WorldUtils.WORLD_SIZE) { [hubPosition] = true };
            UpdateRepulsionField(hubPosition);

            for (int i = 0; i < targetCount; i++)
            {
                // the method below doesn't differentiate between the first path and side branches, so we increment extraPaths to account for the main path
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
        /// Trace a path from the given start tile.
        /// </summary>
        void TracePath(TileData start)
        {
            // debug
            // draw start position
            RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(start.pos), 0.3f));
            //end debug

            bool foundAny = false;
            LinkedList<(TileData tile, Vector2Int[] path, int dist)> stack = new();
            stack.AddFirst((start, new[] { start.pos }, start.dist));
            bool lastFound = false;
            // basically DFS, but once a path is finalized, take the next step from the bottom of the stack
            while (extraPaths_ > 0 && stack.Count > 0)
            {
                WaitForStep(StepType.MicroStep);
                (TileData currentTile, var path, int dist) = lastFound ? stack.First.Value : stack.Last.Value;
                if (lastFound)
                    stack.RemoveFirst();
                else
                    stack.RemoveLast();
                lastFound = PathStep(currentTile, path, dist, foundAny, stack);
                if (lastFound)
                    foundAny = true;
            }

            WaitForStep(StepType.MicroStep);
        }

        /// <summary>
        /// Joins a path to an existing one if possible and returns true.
        /// Otherwise, finds all valid one-tile continuations, ordered by how good they are and appends them to the stack.
        /// </summary>
        bool PathStep(TileData tile, Vector2Int[] path, int dist, bool foundAny, LinkedList<(TileData tile, Vector2Int[] path, int dist)> stack)
        {
            // debug
            // draw the current position and all the path segments leading up to it
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
            // end debug

            // if this tile already has a path, check if the path can join to it
            if (hasPath_[tile.pos])
                return TryJoinPath(tile, path, dist, !foundAny);

            // add valid neighbors to the stack
            foreach (var neighbor in FindValidContinuations(tile, path))
            {
                var newPath = new Vector2Int[path.Length + 1];
                Array.Copy(path, newPath, path.Length);
                newPath[^1] = neighbor.pos;
                stack.AddLast((neighbor, newPath, dist - 1));
            }

            return false;
        }

        /// <summary>
        /// Find all valid continuations of the path, ordered by how preferable they are, most preferable last.
        /// </summary>
        List<TileData> FindValidContinuations(TileData tile, Vector2Int[] path)
        {
            // the next tile must be closer to the hub
            var validNeighborsTemp = tile.neighbors.Where(n => n is not null && n.dist < tile.dist);
            // order them by repulsion (trying to avoid other paths)
            var validNeighbors = validNeighborsTemp.OrderByDescending(n => repulsion_[n.pos]).ToList();

            if (path.Length <= 1)
                return validNeighbors;

            // if there is an option that goes straight, prioritize it
            var straight = validNeighbors.Find(n => n.pos.x == path[^2].x || n.pos.y == path[^2].y);
            if (straight != null)
            {
                validNeighbors.Remove(straight);
                validNeighbors.Add(straight);
            }

            // even more importantly, prioritize options where distance decreases exactly by one
            var exact = validNeighbors.Where(n => n.dist == tile.dist - 1).ToArray();
            foreach (var n in exact)
            {
                validNeighbors.Remove(n);
                validNeighbors.Add(n);
            }

            return validNeighbors;
        }

        /// <summary>
        /// Check whether a path can be joined to a previous path and connect it. The first path can always connect to the hub tile, as long as it's the correct length.
        /// </summary>
        bool TryJoinPath(TileData tile, Vector2Int[] path, int dist, bool isFirst)
        {
            // check for correct length
            if (tile.dist != dist)
                return false;

            // reject the branch if it doesn't go through a tile that's not a neighbor of any other path
            // but only when we've already found a path
            if (!isFirst && path.All(p => outlined_[p]))
                return false;

            // otherwise accept it
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

                    // debug
                    // draw the finished path in cyan for the rest of the phase
                    RegisterGizmos(StepType.Phase, () => new GizmoManager.Line(Color.cyan, WorldUtils.TilePosToWorldPos(pos), WorldUtils.TilePosToWorldPos(prev.pos)));
                    // end debug
                }

                prev = current;
            }

            extraPaths_--;

            // debug
            WaitForStep(StepType.Step);
            // draw all the tiles which are not a neighbor of any path
            RegisterGizmos(StepType.Step, () => outlined_.IndexedEnumerable.Where(p => !p.value).Select(p => new GizmoManager.Cube(Color.yellow, WorldUtils.TilePosToWorldPos(p.index), 0.15f)));
            // end debug
            return true;
        }

        /// <summary>
        /// Increases the strength of the repulsion field at tiles close to the provided tile, bigger values closer to the pathNode.
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