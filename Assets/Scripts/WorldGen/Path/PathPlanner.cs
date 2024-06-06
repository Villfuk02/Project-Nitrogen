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
        static readonly float ChangeCrowdingDiagonal = 2 * GetCrowdingBetween(Vector2Int.zero, Vector2Int.one) - 2;
        static readonly float ChangeCrowdingCardinal = 2 * GetCrowdingBetween(Vector2Int.zero, Vector2Int.up * 2) - 2;

        [Header("Settings")]
        [SerializeField] float[] baseCrowdingByDistanceFromTheEdge;
        [SerializeField] int startCrowdingPenalty;
        [SerializeField] float startTemperature;
        [SerializeField] float endTemperature;
        [SerializeField] float stepsPerLength;

        [Header("Runtime variables")]
        [SerializeField] int stepsLeft;
        [SerializeField] float temperature;
        int totalSteps_;
        Array2D<float> staticCrowding_;
        Array2D<int> distances_;
        Array2D<float> dynamicCrowding_;
        (LinkedListNode<Vector2Int> from, Vector2Int to)[] possibleChanges_;

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

            int totalLength = pathLengths.Sum();
            totalSteps_ = Mathf.FloorToInt(stepsPerLength * totalLength);

            dynamicCrowding_ = new(WorldUtils.WORLD_SIZE);
            InitializeStaticCrowding(starts, hubPosition);

            distances_ = new(WorldUtils.WORLD_SIZE);
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                distances_[v] = v.ManhattanDistance(hubPosition);
        }

        void InitializeStaticCrowding(IEnumerable<Vector2Int> starts, Vector2Int hubPosition)
        {
            staticCrowding_ = new(WorldUtils.WORLD_SIZE);
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                staticCrowding_[v] = baseCrowdingByDistanceFromTheEdge[DistanceFromTheEdge(v)];
            staticCrowding_[hubPosition] += startCrowdingPenalty;
            foreach (var start in starts)
                staticCrowding_[start] += startCrowdingPenalty;
        }

        /// <summary>
        /// Create path prototypes.
        /// </summary>
        public LinkedList<Vector2Int>[] PrototypePaths(Vector2Int[] starts, int[] pathLengths)
        {
            print("Prototyping paths");

            var paths = new LinkedList<Vector2Int>[starts.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                // debug
                // step
                WaitForStep(StepType.MicroStep);
                // end debug

                paths[i] = MakePathPrototype(starts[i], pathLengths[i]);
            }

            print("Finished prototyping");
            return paths;
        }

        /// <summary>
        /// Refine the paths using simulated annealing, still only as a guide for next generation steps.
        /// </summary>
        public Vector2Int[][] RefinePaths(LinkedList<Vector2Int>[] prototypes)
        {
            stepsLeft = totalSteps_;

            // debug
            WaitForStep(StepType.Step);
            print($"Annealing paths in {stepsLeft} steps");
            // draw path prototypes
            RegisterGizmos(StepType.MicroStep, () => prototypes.SelectMany(p => MakePathGizmos(p, Color.red)));
            // end debug

            Vector2Int[][] lastValidPaths = null;
            while (stepsLeft > 0)
            {
                stepsLeft--;
                temperature = Mathf.Lerp(endTemperature, startTemperature, stepsLeft / (float)totalSteps_);

                // if unable to change anything anymore, return early (should only happen when the temperature is way too low)
                if (!TryDoAnnealingStep(prototypes, out var changed))
                    break;
                if (!changed)
                    continue;

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

                    float weight = Mathf.Min(1, 1f / (staticCrowding_[neighbor] + dynamicCrowding_[neighbor]));
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
                length--;
                current = next;
            }

            foreach (var pos in path)
                ApplyCrowding(pos, 1);

            return path;
        }

        /// <summary>
        /// Randomly switch the position of one path node, with higher chance to switch it to a position that makes the path nodes more spread out.
        /// </summary>
        bool TryDoAnnealingStep(IEnumerable<LinkedList<Vector2Int>> paths, out bool changed)
        {
            changed = false;
            possibleChanges_ ??= GetPossibleChanges(paths);
            if (possibleChanges_.Length == 0)
                return false;

            var index = WorldGenerator.Random.Int(possibleChanges_.Length);
            var change = possibleChanges_[index];
            var probability = GetAcceptanceProbability(change.from.Value, change.to);

            if (!WorldGenerator.Random.Bool(probability))
                return true;

            // debug
            // show the change we will preform
            RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.red, WorldUtils.TilePosToWorldPos(change.from.Value), 0.4f));
            RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.yellow, WorldUtils.TilePosToWorldPos(change.to), 0.4f));
            WaitForStep(StepType.MicroStep);
            ExpireGizmos(RelaxPathGizmosDuration);
            // end debug

            // apply the change
            ApplyCrowding(change.to, 1);
            ApplyCrowding(change.from.Value, -1);
            change.from.Value = change.to;
            possibleChanges_ = null;

            changed = true;
            return true;
        }


        static (LinkedListNode<Vector2Int> from, Vector2Int to)[] GetPossibleChanges(IEnumerable<LinkedList<Vector2Int>> paths)
        {
            List<(LinkedListNode<Vector2Int> from, Vector2Int to)> candidates = new();
            foreach (var path in paths)
            {
                var prev = path.First;
                var current = prev.Next;
                var next = current?.Next;
                while (next is not null)
                {
                    var pos = current.Value;

                    // if the prev and next node are on the same tile, three changes are possible
                    if (prev.Value == next.Value)
                        candidates.AddRange(WorldUtils.CARDINAL_DIRS.Where(dir => prev.Value + dir != pos).Select(dir => (current, prev.Value + dir)));

                    // if the nodes don't form a straight line, the path forms a bend and only one change is possible
                    else if (pos - prev.Value != next.Value - pos)
                        candidates.Add((current, prev.Value + next.Value - pos));

                    prev = current;
                    current = next;
                    next = next.Next;
                }
            }

            // remove positions out of bounds
            return candidates.Where(p => WorldUtils.IsInRange(p.to, WorldUtils.WORLD_SIZE)).ToArray();
        }

        float GetAcceptanceProbability(Vector2Int from, Vector2Int to)
        {
            var offset = to - from;
            var crowdingChange = offset.x * offset.y == 0 ? ChangeCrowdingCardinal : ChangeCrowdingDiagonal;
            var improvement = staticCrowding_[from] - staticCrowding_[to] + 2 * dynamicCrowding_[from] - 2 * dynamicCrowding_[to] + crowdingChange;
            return improvement + temperature;
        }

        /// <summary>
        /// Tries to find a crossing (two orthogonal straight passes through the same tile) in the path.
        /// If there is one, reverses a portion of the path to produce two curved passes instead.
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

        void ApplyCrowding(Vector2Int origin, float multiplier)
        {
            foreach (var pos in WorldUtils.WORLD_SIZE)
                dynamicCrowding_[pos] += multiplier * GetCrowdingBetween(pos, origin);
        }

        static float GetCrowdingBetween(Vector2Int t, Vector2Int u)
        {
            float sqrDist = (t - u).sqrMagnitude;
            return 1 / (sqrDist * sqrDist + 1);
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