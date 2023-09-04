using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using Utils.Random;
using static WorldGen.WorldGenerator;

namespace WorldGen.Path
{
    public class PathPlanner : MonoBehaviour
    {
        static readonly object RelaxPathGizmosDuration = new();

        [Header("Settings")]
        [SerializeField] int[] crowdingPenaltyByDistance;
        [SerializeField] int startCrowdingPenalty;
        [SerializeField] float alignmentBonus;
        [SerializeField] float startTemperature;
        [SerializeField] float endTemperature;
        [SerializeField] float stepsPerUnitLengthSquared;

        [FormerlySerializedAs("steps")]
        [Header("Runtime variables")]
        [SerializeField] int stepsLeft;
        [SerializeField] float temperature;
        Array2D<int> crowdingPenaltyTemplate_;
        Vector2Int crowdingPenaltyMaskOffset_;
        Array2D<int> crowding_;

        /// <summary>
        /// Plan the paths, only as a guide for next generation steps.
        /// </summary>
        public Vector2Int[][] PlanPaths(Vector2Int[] starts, int[] pathLengths)
        {
            WaitForStep(StepType.Phase);
            Debug.Log("Planning paths");

            RegisterGizmos(StepType.Phase, () => new List<Vector2Int>(starts) { WorldUtils.WORLD_CENTER }.Select(p => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(p), 0.4f)));

            if (pathLengths.Length == 0)
                return Array.Empty<Vector2Int[]>();

            //prepare variables
            int totalLength = pathLengths.Sum();
            stepsLeft = (int)(stepsPerUnitLengthSquared * totalLength * totalLength);
            temperature = startTemperature;
            float cooling = (startTemperature - endTemperature) / (stepsLeft - 1);
            crowdingPenaltyTemplate_ = new(new(3, 3));
            foreach (var offset in WorldUtils.ADJACENT_AND_ZERO)
                crowdingPenaltyTemplate_[offset + Vector2Int.one] += crowdingPenaltyByDistance[offset.ManhattanMagnitude()];
            crowdingPenaltyMaskOffset_ = -Vector2Int.one;
            crowding_ = new(WorldUtils.WORLD_SIZE);

            int pathCount = pathLengths.Length;
            var distances = new Array2D<int>(WorldUtils.WORLD_SIZE);
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                distances[v] = v.ManhattanDistance(WorldUtils.WORLD_CENTER);

            //make a prototype for each path
            var paths = new LinkedList<Vector2Int>[pathCount];
            for (int i = 0; i < pathCount; i++)
            {
                WaitForStep(StepType.MicroStep);
                paths[i] = MakePathPrototype(starts[i], pathLengths[i], distances);
            }

            Debug.Log($"Path prototypes picked, relaxing in {stepsLeft} steps");
            WaitForStep(StepType.Step);
            RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(p => DrawPath(p, Color.red)));

            //relax paths, trying to get it converge to valid paths with nice shapes
            Vector2Int[][] found = null;
            while (stepsLeft > 0)
            {
                //if unable to relax any more, return early (should only happen when the temperature is way too low)
                if (!RelaxPaths(paths))
                    break;

                //if any path intersects with itself, check for any crossings and try to untwist them
                //RelaxPaths is unlikely to fix a path crossing itself
                if (paths.Any(p => !p.AllDistinct()))
                {
                    RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(p => DrawPath(p, Color.red)), RelaxPathGizmosDuration);
                    foreach (var path in paths)
                        UntwistCrossing(path);
                }
                //if there are no invalid crossings, save the paths
                else if (!InvalidCrossings(paths))
                {
                    RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(p => DrawPath(p, Color.green)), RelaxPathGizmosDuration);
                    found = paths.Select(p => p.ToArray()).ToArray();
                }
                else
                {
                    RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(p => DrawPath(p, Color.yellow)), RelaxPathGizmosDuration);
                }

                temperature -= cooling;
                stepsLeft--;
                WaitForStep(StepType.MicroStep);
            }

            if (found == null)
            {
                Debug.Log("Planning paths failed");
            }
            else
            {
                Debug.Log("Paths planned");
                RegisterGizmos(StepType.Phase, () => found.SelectMany(p => DrawPath(p, Color.green)));
            }

            return found;
        }
        /// <summary>
        /// Make a random path of the correct length form start to center, allowing for all kinds of intersections.
        /// </summary>
        /// <param name="start">Start of the path.</param>
        /// <param name="length">Length of the path.</param>
        /// <param name="distance">Manhattan distance of each tile to the center.</param>
        LinkedList<Vector2Int> MakePathPrototype(Vector2Int start, int length, IReadOnlyArray2D<int> distance)
        {
            var path = new LinkedList<Vector2Int>();
            path.AddFirst(start);

            //for each direction calculate how much it's aligned with the direction from start to center, in the range [0..1]
            var directionToCenter = ((Vector2)(WorldUtils.WORLD_CENTER - start)).normalized;
            var alignment = WorldUtils.CARDINAL_DIRS.Map(d => (1 + Vector2.Dot(d, directionToCenter)) / 2);

            crowding_.Add(crowdingPenaltyTemplate_, start + crowdingPenaltyMaskOffset_);
            crowding_[start] += startCrowdingPenalty;

            var current = start;
            while (length > 0)
            {
                var validNeighbors = new WeightedRandomSet<Vector2Int>(WorldGenerator.Random.NewSeed());
                for (int d = 0; d < 4; d++)
                {
                    var dir = WorldUtils.CARDINAL_DIRS[d];
                    Vector2Int neighbor = current + dir;
                    //skip the tiles that would be too far from the center to get there in time
                    if (distance.TryGet(neighbor, out int dist) && dist <= length)
                        validNeighbors.Add(neighbor, 1f / (crowding_[neighbor] + 1) + alignmentBonus * alignment[d]);
                }

                Vector2Int next = validNeighbors.PopRandom();
                RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.red, WorldUtils.TilePosToWorldPos(current), 0.3f));
                RegisterGizmos(StepType.Step, () => new GizmoManager.Line(Color.red, WorldUtils.TilePosToWorldPos(current), WorldUtils.TilePosToWorldPos(next)));
                WaitForStep(StepType.MicroStep);

                path.AddLast(next);
                crowding_.Add(crowdingPenaltyTemplate_, next + crowdingPenaltyMaskOffset_);
                length--;
                current = next;
            }
            return path;
        }
        /// <summary>
        /// Relax paths, trying to get it converge to valid paths with nice shapes.
        /// </summary>
        bool RelaxPaths(IEnumerable<LinkedList<Vector2Int>> paths)
        {
            //all the offsets with manhattan length of two
            var dirsToTry = WorldUtils.DIAGONAL_DIRS.Concat(WorldUtils.CARDINAL_DIRS.Select(d => 2 * d)).ToArray();

            var possibleChanges = new WeightedRandomSet<(LinkedListNode<Vector2Int>, Vector2Int)>(WorldGenerator.Random.NewSeed());
            //find all path nodes that can be switched to another position (and the switch has positive weight)
            foreach (var path in paths)
            {
                var prev = path.First;
                var current = prev.Next;
                var next = current?.Next;
                while (next is not null)
                {
                    var pos = current.Value;
                    foreach (var offset in dirsToTry)
                    {
                        var newPos = pos + offset;
                        //all nodes still have to be 1 tile apart, so only consider switching when this holds
                        if (newPos.ManhattanDistance(prev.Value) != 1 || newPos.ManhattanDistance(next.Value) != 1)
                            continue;

                        //only consider switching when it the new position is less crowded, the better the improvement the better the weight
                        //but temperature is added, so with high temperature, even very bad switches are likely - this allows for more exploration
                        //when the temperature eventually gets low, only the highest quality switches happen and the path doesn't change much
                        float improvement = crowding_.GetOrDefault(pos, int.MaxValue) - crowding_.GetOrDefault(newPos, int.MaxValue) + temperature;
                        if (improvement > 0)
                            possibleChanges.Add((current, newPos), improvement);
                    }
                    prev = current;
                    current = next;
                    next = next.Next;
                }
            }

            if (possibleChanges.Count == 0)
                return false;

            var (node, newNodePos) = possibleChanges.PopRandom();
            RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.red, WorldUtils.TilePosToWorldPos(node.Value), 0.4f));
            RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.yellow, WorldUtils.TilePosToWorldPos(newNodePos), 0.4f));
            WaitForStep(StepType.MicroStep);
            ExpireGizmos(RelaxPathGizmosDuration);
            //make the chosen switch
            crowding_.Add(crowdingPenaltyTemplate_, newNodePos - Vector2Int.one);
            crowding_.Subtract(crowdingPenaltyTemplate_, node.Value - Vector2Int.one);
            node.Value = newNodePos;
            return true;
        }
        /// <summary>
        /// Tries to find a crossing (two orthogonal straight passages through the same tile) in the path.
        /// If there is one, reverses a portion of the path to produce two curved passes instead.
        /// </summary>
        static void UntwistCrossing(LinkedList<Vector2Int> path)
        {
            var straightX = new Dictionary<Vector2Int, LinkedListNode<Vector2Int>>();
            var straightY = new Dictionary<Vector2Int, LinkedListNode<Vector2Int>>();
            var prev = (LinkedListNode<Vector2Int>)null;
            var current = path.First;
            var next = current?.Next;
            while (current is not null)
            {
                var pos = current.Value;
                Vector2Int dir;
                if (prev == null)
                {
                    dir = pos - next!.Value;
                }
                else if (next == null)
                {
                    dir = pos - prev!.Value;
                }
                else
                {
                    Vector2Int incoming = prev.Value - pos;
                    Vector2Int outgoing = pos - next.Value;
                    if (incoming != outgoing)
                    {
                        prev = current;
                        current = next;
                        next = next.Next;
                        continue;
                    }
                    dir = incoming;
                }
                if (dir.x == 0)
                {
                    straightY[pos] = current;
                    if (straightX.TryGetValue(pos, out var twistStart))
                    {
                        RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.cyan, WorldUtils.TilePosToWorldPos(pos), 0.4f));
                        path.Reverse(twistStart, current);
                        return;
                    }
                }
                else if (dir.y == 0)
                {
                    straightX[pos] = current;
                    if (straightY.TryGetValue(pos, out var twistStart))
                    {
                        RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.cyan, WorldUtils.TilePosToWorldPos(pos), 0.4f));
                        path.Reverse(twistStart, current);
                        return;
                    }
                }

                prev = current;
                current = next;
                next = next?.Next;
            }
        }

        /// <summary>
        /// Are there any invalid crossings?
        /// An invalid crossing means there are two path nodes on the same tile, each with a different distance to the center.
        /// </summary>
        static bool InvalidCrossings(IEnumerable<LinkedList<Vector2Int>> paths)
        {
            var distances = new Dictionary<Vector2Int, int>();
            foreach (var path in paths)
            {
                int distance = 0;
                var current = path.Last;
                while (current != null)
                {
                    if (distances.TryGetValue(current.Value, out int otherDistance) && otherDistance != distance)
                        return true;
                    distances[current.Value] = distance;
                    distance++;
                    current = current.Previous;
                }
            }

            return false;
        }

        static IEnumerable<GizmoManager.GizmoObject> DrawPath(IEnumerable<Vector2Int> path, Color color)
        {
            var gizmos = new List<GizmoManager.GizmoObject>();
            Vector2Int? last = null;
            foreach (var pos in path)
            {
                gizmos.Add(new GizmoManager.Cube(color, WorldUtils.TilePosToWorldPos(pos), 0.1f));
                if (last is { } lastPos)
                    gizmos.Add(new GizmoManager.Line(color, WorldUtils.TilePosToWorldPos(pos), WorldUtils.TilePosToWorldPos(lastPos)));
                last = pos;
            }
            return gizmos;
        }
    }
}
