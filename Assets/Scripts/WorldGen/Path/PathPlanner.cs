using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        [SerializeField] int[] baseCrowdingByDistanceFromTheEdge;
        [SerializeField] int startCrowdingPenalty;
        [SerializeField] float alignmentBonus;
        [SerializeField] float startTemperature;
        [SerializeField] float endTemperature;
        [SerializeField] float stepsPerLengthSquared;
        [SerializeField] float untanglingStepsPerLengthSquared;

        [Header("Runtime variables")]
        [SerializeField] int stepsLeft;
        [SerializeField] int untanglingStepsLeft;
        [SerializeField] float temperature;
        Array2D<int> crowdingPenaltyKernel_;
        static readonly Vector2Int CrowdingPenaltyKernelOffset = -Vector2Int.one;
        Array2D<int> crowding_;
        Array2D<int> distances_;
        int pathCount_;
        Vector2Int[] pathStarts_;
        int[] pathLengths_;
        int mainPhaseSteps_;
        int untanglingSteps_;

        /// <summary>
        /// Initialize the data structures.
        /// </summary>
        public void Init(Vector2Int[] starts, int[] pathLengths, Vector2Int hubPosition)
        {
            // debug
            // step
            WaitForStep(StepType.Phase);
            print("Planning paths");
            // gizmos - path starts and hub position
            RegisterGizmos(StepType.Phase, () => new List<Vector2Int>(starts) { hubPosition }.Select(p => new GizmoManager.Cube(Color.magenta, WorldUtils.TilePosToWorldPos(p), 0.4f)));
            // end debug

            pathCount_ = pathLengths.Length;
            pathStarts_ = starts;
            pathLengths_ = pathLengths;

            int totalLength = pathLengths.Sum();
            mainPhaseSteps_ = Mathf.FloorToInt(stepsPerLengthSquared * totalLength * totalLength);
            untanglingSteps_ = Mathf.FloorToInt(untanglingStepsPerLengthSquared * totalLength * totalLength);

            crowdingPenaltyKernel_ = new(new(3, 3));
            foreach (var offset in WorldUtils.ADJACENT_AND_ZERO)
                crowdingPenaltyKernel_[offset + Vector2Int.one] += crowdingPenaltyByDistance[offset.ManhattanMagnitude()];

            InitializeCrowding();

            distances_ = new(WorldUtils.WORLD_SIZE);
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                distances_[v] = v.ManhattanDistance(hubPosition);
        }

        void InitializeCrowding()
        {
            crowding_ = new(WorldUtils.WORLD_SIZE);
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                crowding_[v] = baseCrowdingByDistanceFromTheEdge[DistanceFromTheEdge(v)];
            foreach (var start in pathStarts_)
            {
                crowding_.Add(crowdingPenaltyKernel_, start + CrowdingPenaltyKernelOffset);
                crowding_[start] += startCrowdingPenalty;
            }
        }

        /// <summary>
        /// Create path prototypes.
        /// </summary>
        public LinkedList<Vector2Int>[] PrototypePaths()
        {
            print("Prototyping paths");

            var paths = new LinkedList<Vector2Int>[pathCount_];
            for (int i = 0; i < pathCount_; i++)
            {
                // debug
                // step
                WaitForStep(StepType.MicroStep);
                // end debug

                paths[i] = MakePathPrototype(pathStarts_[i], pathLengths_[i]);
            }

            print("Finished prototyping");
            return paths;
        }

        /// <summary>
        /// Refine the paths using simulated annealing, still only as a guide for next generation steps.
        /// </summary>
        public Vector2Int[][] RefinePaths(LinkedList<Vector2Int>[] prototypes)
        {
            untanglingStepsLeft = untanglingSteps_;
            stepsLeft = mainPhaseSteps_ + untanglingStepsLeft;
            temperature = startTemperature;

            // debug
            WaitForStep(StepType.Step);
            print($"Relaxing paths in {stepsLeft} steps");
            // draw path prototypes
            RegisterGizmos(StepType.MicroStep, () => prototypes.SelectMany(p => MakePathGizmos(p, Color.red)));
            // end debug

            Vector2Int[][] lastValidPaths = null;
            while (stepsLeft > 0)
            {
                // if unable to relax any more, return early (should only happen when the temperature is way too low)
                if (!DoRelaxationStep(prototypes))
                    break;

                // check if untangling was successful - any valid paths have been found. If not, fail early
                if (untanglingStepsLeft == 0 && lastValidPaths == null)
                    break;

                // if any path intersects with itself, check for any crossings and try to untwist them
                // relaxing is unlikely to fix a path crossing itself
                if (prototypes.Any(p => !p.AllDistinct()))
                {
                    // debug
                    // draw paths
                    RegisterGizmos(StepType.MicroStep, () => prototypes.SelectMany(p => MakePathGizmos(p, Color.red)), RelaxPathGizmosDuration);
                    // end debug

                    foreach (var path in prototypes)
                        UntwistCrossingIfExists(path);
                }
                else if (CheckInvalidIntersections(prototypes))
                {
                    // debug
                    // draw paths
                    RegisterGizmos(StepType.MicroStep, () => prototypes.SelectMany(p => MakePathGizmos(p, Color.yellow)), RelaxPathGizmosDuration);
                    // end debug
                }
                // if there are no invalid crossings, save the paths
                else
                {
                    // debug
                    // draw paths
                    RegisterGizmos(StepType.MicroStep, () => prototypes.SelectMany(p => MakePathGizmos(p, Color.green)), RelaxPathGizmosDuration);
                    // end debug

                    lastValidPaths = prototypes.Select(p => p.ToArray()).ToArray();
                }

                float progress = untanglingStepsLeft > 0 ? untanglingStepsLeft / (float)untanglingSteps_ : stepsLeft / (float)mainPhaseSteps_;
                temperature = Mathf.Lerp(endTemperature, startTemperature, progress);
                stepsLeft--;
                untanglingStepsLeft--;

                // debug
                WaitForStep(StepType.MicroStep);
                // end debug
            }

            if (lastValidPaths == null)
            {
                print("Planning paths failed");
            }
            else
            {
                // debug
                print("Paths planned");
                // draw the paths selected in the end
                RegisterGizmos(StepType.Phase, () => lastValidPaths.SelectMany(p => MakePathGizmos(p, Color.green)));
                // end debug
            }

            return lastValidPaths;
        }

        static int DistanceFromTheEdge(Vector2Int tilePos)
        {
            return Mathf.Min(tilePos.x, tilePos.y, WorldUtils.WORLD_SIZE.x - tilePos.x - 1, WorldUtils.WORLD_SIZE.y - tilePos.y - 1);
        }

        /// <summary>
        /// Make a random path of the correct length form start to the hub, allowing for all kinds of intersections.
        /// </summary>
        LinkedList<Vector2Int> MakePathPrototype(Vector2Int start, int length)
        {
            var path = new LinkedList<Vector2Int>();
            path.AddFirst(start);

            // for each direction calculate how much it's aligned with the direction from start to center, in the range [0..1]
            var directionToCenter = ((Vector2)(WorldUtils.WORLD_CENTER - start)).normalized;
            var alignment = WorldUtils.CARDINAL_DIRS.Map(d => (1 + Vector2.Dot(d, directionToCenter)) / 2);

            var current = start;
            while (length > 0)
            {
                var validNeighbors = new WeightedRandomSet<Vector2Int>(WorldGenerator.Random.NewSeed());
                for (int d = 0; d < 4; d++)
                {
                    var dir = WorldUtils.CARDINAL_DIRS[d];
                    Vector2Int neighbor = current + dir;
                    // skip the tiles that would be too far from the hub to get there in time
                    if (!distances_.TryGet(neighbor, out int dist) || dist > length)
                        continue;

                    float weight = 1f / crowding_[neighbor] + alignmentBonus * alignment[d];
                    validNeighbors.AddOrUpdate(neighbor, weight);
                }

                Vector2Int next = validNeighbors.PopRandom();

                // debug
                // draw current node and a line connecting it to the next node
                RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.red, WorldUtils.TilePosToWorldPos(current), 0.3f));
                RegisterGizmos(StepType.Step, () => new GizmoManager.Line(Color.red, WorldUtils.TilePosToWorldPos(current), WorldUtils.TilePosToWorldPos(next)));
                WaitForStep(StepType.MicroStep);
                //end debug

                path.AddLast(next);
                crowding_.Add(crowdingPenaltyKernel_, next + CrowdingPenaltyKernelOffset);
                length--;
                current = next;
            }

            return path;
        }

        /// <summary>
        /// Randomly switch the position of one path node, with higher chance to switch it to a position that makes the path nodes more spread out.
        /// </summary>
        bool DoRelaxationStep(IEnumerable<LinkedList<Vector2Int>> paths)
        {
            // all the offsets with manhattan length of two
            var dirsToTry = WorldUtils.DIAGONAL_DIRS.Concat(WorldUtils.CARDINAL_DIRS.Select(d => 2 * d)).ToArray();

            var possibleChanges = new WeightedRandomSet<(LinkedListNode<Vector2Int>, Vector2Int)>(WorldGenerator.Random.NewSeed());
            // find all path nodes that can be switched to another position (and the switch has positive weight)
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
                        // all nodes still have to be 1 tile apart, so only consider switching when this holds
                        if (newPos.ManhattanDistance(prev.Value) != 1 || newPos.ManhattanDistance(next.Value) != 1)
                            continue;

                        // only consider switching it when the new position is less crowded
                        // the better the improvement the better the weight
                        // but temperature is added, so with high temperature, even very bad switches are likely - this allows for more exploration
                        // when the temperature eventually gets low, only the highest quality switches happen and the path doesn't change much
                        float improvement = crowding_.GetOrDefault(pos, int.MaxValue) - crowding_.GetOrDefault(newPos, int.MaxValue);
                        improvement += temperature;
                        if (improvement > 0)
                            possibleChanges.AddOrUpdate((current, newPos), improvement);
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
            // make the chosen switch
            crowding_.Add(crowdingPenaltyKernel_, newNodePos - Vector2Int.one);
            crowding_.Subtract(crowdingPenaltyKernel_, node.Value - Vector2Int.one);
            node.Value = newNodePos;
            return true;
        }

        /// <summary>
        /// Tries to find a crossing (two orthogonal straight passages through the same tile) in the path.
        /// If there is one, reverses a portion of the path to produce two curved passages instead.
        /// </summary>
        static void UntwistCrossingIfExists(LinkedList<Vector2Int> path)
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
                        // debug
                        // draw where the untwisting happened
                        RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.cyan, WorldUtils.TilePosToWorldPos(pos), 0.4f));
                        // end debug
                        path.Reverse(twistStart, current);
                        return;
                    }
                }
                else if (dir.y == 0)
                {
                    straightX[pos] = current;
                    if (straightY.TryGetValue(pos, out var twistStart))
                    {
                        // debug
                        // draw where the untwisting happened
                        RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.cyan, WorldUtils.TilePosToWorldPos(pos), 0.4f));
                        // end debug
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
        /// An invalid crossing is when two path nodes on the same tile each have a different distance to the hub.
        /// </summary>
        static bool CheckInvalidIntersections(IEnumerable<LinkedList<Vector2Int>> paths)
        {
            var distances = new Dictionary<Vector2Int, int>();
            foreach (var path in paths)
            {
                int distance = path.Count;
                var current = path.First;
                while (current != null)
                {
                    bool invalid = distances.TryGetValue(current.Value, out var otherDistance) && otherDistance != distance;
                    if (invalid)
                        return true;

                    distances[current.Value] = distance;
                    distance--;
                    current = current.Next;
                }
            }

            return false;
        }

        static IEnumerable<GizmoManager.GizmoObject> MakePathGizmos(IEnumerable<Vector2Int> path, Color color)
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