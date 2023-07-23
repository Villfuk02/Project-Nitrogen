using Random;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Utils;
using static Utils.TimingUtils;
using static WorldGen.WorldGenerator;

namespace WorldGen.Path
{
    public class PathPlanner : MonoBehaviour
    {
        [Header("Picking Path Starts")]
        [SerializeField] float startSpacingMultiplier;

        [Header("Planning Paths")]
        [SerializeField] int[] crowdingPenaltyByDistance;
        [SerializeField] int startCrowdingPenalty;
        [SerializeField] float startTemperature;
        [SerializeField] float endTemperature;
        [SerializeField] float stepsPerUnitLengthSquared;

        [Header("Finalizing Paths")]
        [SerializeField] int maxPathsPerStart;
        [SerializeField] int minMergeDistance;

        //Runtime variables
        static int[] pathLengths_;
        Random.Random random_;
        static Array2D<int> crowdingPenaltyMask_;

        public void Init(int[] pathLengths, ulong randomSeed)
        {
            pathLengths_ = pathLengths;
            random_ = new(randomSeed);
            crowdingPenaltyMask_ = new(new(3, 3));
            foreach (var offset in WorldUtils.ADJACENT_AND_ZERO)
            {
                crowdingPenaltyMask_[offset + Vector2Int.one] += crowdingPenaltyByDistance[offset.ManhattanMagnitude()];
            }
        }

        static bool IsOnEdge(Vector2Int pos)
        {
            return pos.x == 0 || pos.x == WorldUtils.WORLD_SIZE.x - 1 || pos.y == 0 || pos.y == WorldUtils.WORLD_SIZE.y - 1;
        }
        public JobDataInterface PickStarts(out Vector2Int[] starts)
        {
            starts = new Vector2Int[pathLengths_.Length];
            JobDataInterface jobData = new(Allocator.Persistent);
            JobHandle handle = new PickStartsJob
            {
                picked = jobData.Register(starts, JobDataInterface.Mode.Output),
                randomSeed = random_.NewSeed(),
                startSpacingMultiplier = startSpacingMultiplier
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }

        struct PickStartsJob : IJob
        {
            public NativeArray<Vector2Int> picked;
            public ulong randomSeed;
            public float startSpacingMultiplier;
            public void Execute()
            {
                WaitForStep(StepType.Phase);
                Debug.Log("Picking Starts");

                RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(WorldUtils.ORIGIN), 0.4f));

                WaitForStep(StepType.Step);

                GetPossibleStarts(out var oddStarts, out var evenStarts);

                int pathCount = pathLengths_.Length;
                int perimeter = (WorldUtils.WORLD_SIZE.x + WorldUtils.WORLD_SIZE.y) * 2;
                float minDistSqr = perimeter * startSpacingMultiplier / pathCount;
                minDistSqr *= minDistSqr;
                List<Vector2Int> pickedList = new();
                for (int i = 0; i < pathCount; i++)
                {
                    RegisterGizmos(StepType.MicroStep, () => oddStarts.AllEntries.Concat(evenStarts.AllEntries)
                        .Where(t => pickedList.All(u => (t - u).sqrMagnitude >= minDistSqr))
                        .Select(t => new GizmoManager.Cube(Color.green, WorldUtils.TileToWorldPos(t), 0.3f)));
                    WaitForStep(StepType.MicroStep);
                    var tp = ChooseStart(pathLengths_[i], minDistSqr, pathLengths_[i] % 2 == 0 ? evenStarts : oddStarts, pickedList);
                    picked[i] = tp;
                    pickedList.Add(tp);
                    RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(tp), 0.4f));
                }
            }

            readonly void GetPossibleStarts(out RandomSet<Vector2Int> oddStarts, out RandomSet<Vector2Int> evenStart)
            {
                Random.Random random = new(randomSeed);
                oddStarts = new(random.NewSeed());
                evenStart = new(random.NewSeed());
                foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                {
                    if (!IsOnEdge(v))
                        continue;

                    if ((v.x + v.y - WorldUtils.ORIGIN.x - WorldUtils.ORIGIN.y) % 2 == 0)
                        evenStart.Add(v);
                    else
                        oddStarts.Add(v);
                }
            }

            static Vector2Int ChooseStart(int length, float minDistSqr, RandomSet<Vector2Int> available, IReadOnlyCollection<Vector2Int> currentlyPicked)
            {
                Vector2Int result;
                List<Vector2Int> tooFar = new();
                do
                {
                    result = available.PopRandom();
                    RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.yellow, WorldUtils.TileToWorldPos(result), 0.2f));
                    if (result.ManhattanDistance(WorldUtils.ORIGIN) > length)
                    {
                        tooFar.Add(result);
                        continue;
                    }

                    if (currentlyPicked.All(t => (result - t).sqrMagnitude >= minDistSqr))
                        break;
                } while (true);
                available.AddRange(tooFar);
                return result;
            }
        }

        public JobDataInterface PlanPaths(Vector2Int[] starts, out Vector2Int[] flatPaths)
        {
            int totalLength = pathLengths_.Sum();
            flatPaths = new Vector2Int[totalLength + pathLengths_.Length];
            JobDataInterface jobData = new(Allocator.Persistent);
            int steps = (int)(stepsPerUnitLengthSquared * totalLength * totalLength);
            JobHandle handle = new PlanPathsJob
            {
                starts = jobData.Register(starts, JobDataInterface.Mode.Input),
                returnPaths = jobData.Register(flatPaths, JobDataInterface.Mode.Output),
                startCrowdingPenalty = startCrowdingPenalty,
                temperature = startTemperature,
                cooling = (startTemperature - endTemperature) / (steps - 1),
                steps = steps,
                randomSeed = random_.NewSeed(),
                failed = jobData.RegisterFailed()
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }

        struct PlanPathsJob : IJob
        {
            public NativeArray<Vector2Int> starts;
            public NativeArray<Vector2Int> returnPaths;
            public int startCrowdingPenalty;
            public float temperature;
            public float cooling;
            public int steps;
            public ulong randomSeed;

            public NativeArray<bool> failed;
            public void Execute()
            {
                Debug.Log("Planning paths");
                RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(WorldUtils.ORIGIN), 0.4f));

                Random.Random random = new(randomSeed);

                int pathCount = pathLengths_.Length;
                Array2D<int> distances = new(WorldUtils.WORLD_SIZE);
                foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                    distances[v] = v.ManhattanDistance(WorldUtils.ORIGIN);

                var paths = new LinkedList<Vector2Int>[pathCount];
                Array2D<int> crowding = new(WorldUtils.WORLD_SIZE);
                for (int i = 0; i < pathCount; i++)
                {
                    WaitForStep(StepType.MicroStep);
                    paths[i] = MakePathPrototype(starts[i], pathLengths_[i], random, distances, crowding);
                }

                Debug.Log($"Path prototypes picked, relaxing in {steps} steps");
                WaitForStep(StepType.Step);

                var found = pathCount == 0 ? Array.Empty<Vector2Int>() : null;
                var nodeCounts = pathLengths_.Select(l => l + 1).ToArray();
                while (steps > 0)
                {
                    if (!RelaxPaths(paths, crowding, random.NewSeed()))
                        break;

                    if (paths.Any(p => !p.AllDistinct()))
                    {
                        foreach (var path in paths)
                            UntwistCrossing(path);
                    }
                    else if (!InvalidCrossings(paths))
                    {
                        found = paths.PackFlat(nodeCounts);
                    }

                    temperature -= cooling;
                    steps--;
                    WaitForStep(StepType.MicroStep);
                }

                if (found == null)
                {
                    Debug.Log("Planning paths failed");
                    failed[0] = true;
                    return;
                }

                Debug.Log("Paths planned");
                returnPaths.CopyFrom(found);

                RegisterGizmos(StepType.Phase, () => found.UnpackFlat(nodeCounts).SelectMany(DrawPath));
            }

            LinkedList<Vector2Int> MakePathPrototype(Vector2Int start, int length, Random.Random random, IReadOnlyArray2D<int> distance, Array2D<int> crowding)
            {
                LinkedList<Vector2Int> path = new();
                path.AddFirst(start);

                crowding.AddMask(crowdingPenaltyMask_, start - Vector2Int.one);
                crowding[start] += startCrowdingPenalty;

                var current = start;
                while (length > 0)
                {
                    WeightedRandomSet<Vector2Int> validNeighbors = new(random.NewSeed());
                    foreach (var dir in WorldUtils.CARDINAL_DIRS)
                    {
                        Vector2Int neighbor = current + dir;
                        if (distance.TryGet(neighbor, out var d) && d <= length)
                            validNeighbors.Add(neighbor, 1f / (crowding[neighbor] + 1));
                    }

                    Vector2Int next = validNeighbors.PopRandom();
                    RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.red, WorldUtils.TileToWorldPos(current), 0.3f));
                    RegisterGizmos(StepType.Step, () => new GizmoManager.Line(Color.red, WorldUtils.TileToWorldPos(current), WorldUtils.TileToWorldPos(next)));
                    WaitForStep(StepType.MicroStep);

                    path.AddLast(next);
                    crowding.AddMask(crowdingPenaltyMask_, next - Vector2Int.one);
                    length--;
                    current = next;
                }
                return path;
            }

            bool RelaxPaths(LinkedList<Vector2Int>[] paths, Array2D<int> crowding, ulong seed)
            {
                var dirsToTry = WorldUtils.DIAGONAL_DIRS.Concat(WorldUtils.CARDINAL_DIRS.Select(d => 2 * d)).ToArray();

                RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(DrawPath));

                WeightedRandomSet<(LinkedListNode<Vector2Int>, Vector2Int)> possibleChanges = new(seed);
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

                            float improvement = crowding.GetOrDefault(pos, int.MaxValue) - crowding.GetOrDefault(newPos, int.MaxValue) + temperature;
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

                crowding.AddMask(crowdingPenaltyMask_, newNodePos - Vector2Int.one);
                crowding.SubtractMask(crowdingPenaltyMask_, node.Value - Vector2Int.one);
                node.Value = newNodePos;

                RegisterGizmos(StepType.MicroStep, () => paths.SelectMany(DrawPath));

                return true;
            }

            static void UntwistCrossing(LinkedList<Vector2Int> path)
            {
                Dictionary<Vector2Int, LinkedListNode<Vector2Int>> straightX = new();
                Dictionary<Vector2Int, LinkedListNode<Vector2Int>> straightY = new();
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
                            path.Reverse(twistStart, current);
                            return;
                        }
                    }
                    else if (dir.y == 0)
                    {
                        straightX[pos] = current;
                        if (straightY.TryGetValue(pos, out var twistStart))
                        {
                            path.Reverse(twistStart, current);
                            return;
                        }
                    }

                    RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Line(Color.yellow, WorldUtils.TileToWorldPos(prev?.Value ?? pos), WorldUtils.TileToWorldPos(next?.Value ?? pos)));

                    prev = current;
                    current = next;
                    next = next?.Next;
                }
            }

            static bool InvalidCrossings(IEnumerable<LinkedList<Vector2Int>> paths)
            {
                Dictionary<Vector2Int, int> distances = new();
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
            static IEnumerable<GizmoManager.GizmoObject> DrawPath(IEnumerable<Vector2Int> path)
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                Vector2Int? last = null;
                foreach (var pos in path)
                {
                    gizmos.Add(new GizmoManager.Cube(Color.green, WorldUtils.TileToWorldPos(pos), 0.1f));
                    if (last is { } lastPos)
                        gizmos.Add(new GizmoManager.Line(Color.green, WorldUtils.TileToWorldPos(pos), WorldUtils.TileToWorldPos(lastPos)));
                    last = pos;
                }
                return gizmos;
            }
        }

        public void UnpackPlannedPaths(in Vector2Int[] flatPaths, out Vector2Int[][] paths)
        {
            paths = flatPaths.UnpackFlat(pathLengths_.Select(l => l + 1).ToArray());
        }
        /*
        public JobDataInterface FinalisePaths(Vector2Int[] starts)
        {
            JobDataInterface jobData = new(Allocator.Persistent);

            JobHandle handle = new FinalisePathsJob
            {
                starts = jobData.Register(starts, false),
                maxPathsPerTarget = maxPathsPerTarget,
                minMergeDistance = minMergeDistance
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }
        struct FinalisePathsJob : IJob
        {
            public NativeArray<Vector2Int> starts;
            public int maxPathsPerTarget;
            public int minMergeDistance;
            public void Execute()
            {
                WaitForStep(StepType.Phase);
                Debug.Log("Finalizing Paths");

                RegisterGizmos(StepType.Phase, () =>
                {
                    List<GizmoManager.GizmoObject> gizmos = new();
                    foreach (LevelGenTile tile in Tiles)
                    {
                        Vector3 pos = WorldUtils.TileToWorldPos(tile.pos);
                        if (!tile.passable)
                            gizmos.Add(new GizmoManager.Cube(Color.red, pos, 0.4f));
                        for (int i = 0; i < 4; i++)
                        {
                            if (tile.neighbors[i] is null)
                                gizmos.Add(new GizmoManager.Cube(Color.red, (WorldUtils.TileToWorldPos(tile.pos + WorldUtils.CARDINAL_DIRS[i]) + pos) * 0.5f, 0.25f));
                        }
                    }
                    return gizmos;
                });

                int targetCount = starts.Length;
                bool[,] pathTiles = new bool[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y]; !!! 2d array !!!
                LevelGenTile[] paths = new LevelGenTile[targetCount];
                for (int i = 0; i < targetCount; i++)
                {
                    paths[i] = Tiles[starts[i]];
                    pathTiles[paths[i].pos.x, paths[i].pos.y] = true; !!! 2d array !!!
                }
                for (int i = 0; i < targetCount; i++)
                {
                    WaitForStep(StepType.Step);
                    TracePathQueued(paths[i], maxPathsPerTarget, minMergeDistance);
                }
                WaitForStep(StepType.Step);
                foreach (var tile in Tiles)
                {
                    if (tile.pathNext.Count == 0 && tile.pos != WorldUtils.ORIGIN)
                        tile.dist = int.MaxValue;
                }
                Debug.Log("Paths finalised");
            }

            void TracePathQueued(LevelGenTile t, int pathsLeft, int minMergeDistance)
            {
                RegisterGizmos(StepType.Step, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(t.pos), 0.3f));
                HashSet<Vector2Int> taken = new();
                LinkedList<(LevelGenTile t, Vector2Int[] path, int distToMerge)> queue = new();
                queue.AddFirst((t, new Vector2Int[] { t.pos }, 0));
                bool lastFound = false;
                while (pathsLeft > 0 && queue.Count > 0)
                {
                    WaitForStep(StepType.Substep);
                    (LevelGenTile u, Vector2Int[] path, int distToMerge) = lastFound ? queue.First.Value : queue.Last.Value;
                    if (lastFound)
                        queue.RemoveFirst();
                    else
                        queue.RemoveLast();
                    lastFound = TracePath(u, path, distToMerge);
                }

                bool TracePath(LevelGenTile t, Vector2Int[] path, int distToMerge)
                {
                    RegisterGizmos(StepType.Substep, () =>
                    {
                        List<GizmoManager.GizmoObject> gizmos = new()
                        {
                            new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(t.pos), 0.2f)
                        };
                        LevelGenTile prev = null;
                        foreach (var pos in path)
                        {
                            LevelGenTile current = Tiles[pos];
                            if (prev is not null)
                            {
                                gizmos.Add(new GizmoManager.Line(Color.magenta, WorldUtils.TileToWorldPos(pos), WorldUtils.TileToWorldPos(prev.pos)));
                            }
                            prev = current;
                        }
                        return gizmos;
                    });
                    if (t.dist == 0 || (taken.Contains(t.pos) && distToMerge <= 0))
                    {
                        if (pathsLeft > 0)
                        {
                            pathsLeft--;
                            LevelGenTile prev = null;
                            foreach (var pos in path)
                            {
                                taken.Add(pos);
                                LevelGenTile current = Tiles[pos];
                                if (prev is not null)
                                {
                                    if (!prev.pathNext.Contains(current))
                                        prev.pathNext.Add(current);
                                    RegisterGizmos(StepType.Phase, () => new GizmoManager.Line(Color.cyan, WorldUtils.TileToWorldPos(pos), WorldUtils.TileToWorldPos(prev.pos)));
                                }
                                prev = current;
                            }
                        }
                        return true;
                    }
                    distToMerge--;
                    int count = 0;
                    RandomSet<int> order = new();
                    for (int i = 0; i < 4; i++)
                    {
                        if (t.neighbors[i] is LevelGenTile neighbor && neighbor.dist == t.dist - 1)
                        {
                            if (distToMerge <= 0 || !taken.Contains(neighbor.pos))
                            {
                                count++;
                                order.Add(i);
                            }
                        }
                    }
                    if (count == 0)
                    {
                        return false;
                    }
                    if (count > 1)
                    {
                        distToMerge = minMergeDistance;
                    }
                    while (order.Count > 0)
                    {
                        int c = order.PopRandom();
                        LevelGenTile u = t.neighbors[c];
                        Vector2Int[] newPath = new Vector2Int[path.Length + 1];
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
        */
    }
}
