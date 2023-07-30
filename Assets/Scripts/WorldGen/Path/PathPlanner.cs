using Random;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
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

        [Header("Runtime variables")]
        [SerializeField] int steps;
        [SerializeField] float temperature;
        Array2D<int> crowdingPenaltyMask_;
        Vector2Int crowdingPenaltyMaskOffset_;
        Array2D<int> crowding_;

        public Vector2Int[][] PlanPaths(Vector2Int[] starts, int[] pathLengths)
        {
            WaitForStep(StepType.Phase);
            Debug.Log("Planning paths");

            RegisterGizmos(StepType.Phase, () => new List<Vector2Int>(starts) { WorldUtils.ORIGIN }.Select(p => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(p), 0.4f)));

            int totalLength = pathLengths.Sum();
            steps = (int)(stepsPerUnitLengthSquared * totalLength * totalLength);
            temperature = startTemperature;
            float cooling = (startTemperature - endTemperature) / (steps - 1);
            crowdingPenaltyMask_ = new(new(3, 3));
            foreach (var offset in WorldUtils.ADJACENT_AND_ZERO)
            {
                crowdingPenaltyMask_[offset + Vector2Int.one] += crowdingPenaltyByDistance[offset.ManhattanMagnitude()];
            }
            crowdingPenaltyMaskOffset_ = -Vector2Int.one;
            crowding_ = new(WorldUtils.WORLD_SIZE);

            int pathCount = pathLengths.Length;
            var distances = new Array2D<int>(WorldUtils.WORLD_SIZE);
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                distances[v] = v.ManhattanDistance(WorldUtils.ORIGIN);

            var paths = new LinkedList<Vector2Int>[pathCount];
            for (int i = 0; i < pathCount; i++)
            {
                WaitForStep(StepType.MicroStep);
                paths[i] = MakePathPrototype(starts[i], pathLengths[i], distances);
            }

            Debug.Log($"Path prototypes picked, relaxing in {steps} steps");
            WaitForStep(StepType.Step);
            RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(p => DrawPath(p, Color.red)));

            if (pathCount == 0)
                return Array.Empty<Vector2Int[]>();

            Vector2Int[][] found = null;
            while (steps > 0)
            {
                if (!RelaxPaths(paths))
                    break;

                if (paths.Any(p => !p.AllDistinct()))
                {
                    RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(p => DrawPath(p, Color.yellow)), RelaxPathGizmosDuration);
                    foreach (var path in paths)
                        UntwistCrossing(path);
                }
                else if (!InvalidCrossings(paths))
                {
                    RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(p => DrawPath(p, Color.green)), RelaxPathGizmosDuration);
                    found = paths.Select(p => p.ToArray()).ToArray();
                }
                else
                {
                    RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(p => DrawPath(p, Color.red)), RelaxPathGizmosDuration);
                }

                temperature -= cooling;
                steps--;
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

        LinkedList<Vector2Int> MakePathPrototype(Vector2Int start, int length, IReadOnlyArray2D<int> distance)
        {
            var path = new LinkedList<Vector2Int>();
            path.AddFirst(start);

            var directionToCenter = ((Vector2)(WorldUtils.ORIGIN - start)).normalized;
            var alignment = WorldUtils.CARDINAL_DIRS.Map(d => (1 + Vector2.Dot(d, directionToCenter)) / 2);

            crowding_.AddMask(crowdingPenaltyMask_, start + crowdingPenaltyMaskOffset_);
            crowding_[start] += startCrowdingPenalty;

            var current = start;
            while (length > 0)
            {
                var validNeighbors = new WeightedRandomSet<Vector2Int>(WorldGenerator.Random.NewSeed());
                for (int d = 0; d < 4; d++)
                {
                    var dir = WorldUtils.CARDINAL_DIRS[d];
                    Vector2Int neighbor = current + dir;
                    if (distance.TryGet(neighbor, out int dist) && dist <= length)
                        validNeighbors.Add(neighbor, 1f / (crowding_[neighbor] + 1) + alignmentBonus * alignment[d]);
                }

                Vector2Int next = validNeighbors.PopRandom();
                RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.red, WorldUtils.TileToWorldPos(current), 0.3f));
                RegisterGizmos(StepType.Step, () => new GizmoManager.Line(Color.red, WorldUtils.TileToWorldPos(current), WorldUtils.TileToWorldPos(next)));
                WaitForStep(StepType.MicroStep);

                path.AddLast(next);
                crowding_.AddMask(crowdingPenaltyMask_, next + crowdingPenaltyMaskOffset_);
                length--;
                current = next;
            }
            return path;
        }

        bool RelaxPaths(IEnumerable<LinkedList<Vector2Int>> paths)
        {
            var dirsToTry = WorldUtils.DIAGONAL_DIRS.Concat(WorldUtils.CARDINAL_DIRS.Select(d => 2 * d)).ToArray();

            var possibleChanges = new WeightedRandomSet<(LinkedListNode<Vector2Int>, Vector2Int)>(WorldGenerator.Random.NewSeed());
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
                        if (newPos.ManhattanDistance(prev.Value) != 1 || newPos.ManhattanDistance(next.Value) != 1)
                            continue;

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
            RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.red, WorldUtils.TileToWorldPos(node.Value), 0.4f));
            RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.yellow, WorldUtils.TileToWorldPos(newNodePos), 0.4f));
            WaitForStep(StepType.MicroStep);
            ExpireGizmos(RelaxPathGizmosDuration);

            crowding_.AddMask(crowdingPenaltyMask_, newNodePos - Vector2Int.one);
            crowding_.SubtractMask(crowdingPenaltyMask_, node.Value - Vector2Int.one);
            node.Value = newNodePos;
            return true;
        }

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
                        RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.cyan, WorldUtils.TileToWorldPos(pos), 0.4f));
                        path.Reverse(twistStart, current);
                        return;
                    }
                }
                else if (dir.y == 0)
                {
                    straightX[pos] = current;
                    if (straightY.TryGetValue(pos, out var twistStart))
                    {
                        RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.cyan, WorldUtils.TileToWorldPos(pos), 0.4f));
                        path.Reverse(twistStart, current);
                        return;
                    }
                }

                prev = current;
                current = next;
                next = next?.Next;
            }
        }

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
                gizmos.Add(new GizmoManager.Cube(color, WorldUtils.TileToWorldPos(pos), 0.1f));
                if (last is { } lastPos)
                    gizmos.Add(new GizmoManager.Line(color, WorldUtils.TileToWorldPos(pos), WorldUtils.TileToWorldPos(lastPos)));
                last = pos;
            }
            return gizmos;
        }
    }
}
