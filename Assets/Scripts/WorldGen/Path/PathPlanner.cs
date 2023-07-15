using Random;
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
        int[] pathLengths_;
        Random.Random random_;
        [SerializeField] float startSpacingMultiplier;
        [SerializeField] int maxPathsPerStart;
        [SerializeField] int stepsUntilFail;
        [SerializeField] int minMergeDistance;

        public void Init(int[] pathLengths, ulong randomSeed)
        {
            pathLengths_ = pathLengths;
            random_ = new(randomSeed);
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
                pathLengths = jobData.Register(pathLengths_, false),
                picked = jobData.Register(starts, true),
                randomSeed = random_.NewSeed(),
                startSpacingMultiplier = startSpacingMultiplier
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }

        struct PickStartsJob : IJob
        {
            public NativeArray<int> pathLengths;
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


                int pathCount = pathLengths.Length;
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
                    var tp = ChooseStart(pathLengths[i], minDistSqr, pathLengths[i] % 2 == 0 ? evenStarts : oddStarts, pickedList);
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
            Vector2Int ChooseStart(int length, float minDistSqr, RandomSet<Vector2Int> available, IReadOnlyCollection<Vector2Int> currentlyPicked)
            {
                Vector2Int result;
                List<Vector2Int> tooFar = new();
                do
                {
                    result = available.PopRandom();
                    RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.yellow, WorldUtils.TileToWorldPos(result), 0.2f));
                    Vector2Int relative = result - WorldUtils.ORIGIN;
                    if (Mathf.Abs(relative.x) + Mathf.Abs(relative.y) > length)
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

        public JobDataInterface PickPaths(Vector2Int[] starts, out int[] flatNodes)
        {
            flatNodes = new int[WorldUtils.WORLD_SIZE.x * WorldUtils.WORLD_SIZE.y];
            JobDataInterface jobData = new(Allocator.Persistent);
            JobHandle handle = new PickPathsJob
            {
                pathLengths = jobData.Register(pathLengths_, false),
                starts = jobData.Register(starts, false),
                retNodes = jobData.Register(flatNodes, true),
                stepsUntilFail = stepsUntilFail,
                failed = jobData.RegisterFailed(),
                randomSeed = random_.NewSeed()
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }

        struct PickPathsJob : IJob
        {
            public NativeArray<int> pathLengths;
            public NativeArray<Vector2Int> starts;
            public NativeArray<int> retNodes;
            public NativeArray<bool> failed;
            public int stepsUntilFail;
            public ulong randomSeed;
            public void Execute()
            {
                Debug.Log("Picking Paths");

                int pathCount = pathLengths.Length;
                int[,] nodes = new int[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
                foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                    nodes[v.x, v.y] = int.MaxValue;
                int startPos = pathCount <= 4 ? 1 : 2;
                nodes[WorldUtils.ORIGIN.x, WorldUtils.ORIGIN.y] = 0;
                if (startPos > 1)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2Int p = WorldUtils.ORIGIN + WorldUtils.CARDINAL_DIRS[i];
                        nodes[p.x, p.y] = 1;
                    }
                }

                Random.Random random = new(randomSeed);
                WeightedRandomSet<PlannedPath> queue = new(random.NewSeed());
                HashSet<((Vector2Int, int) prev, (Vector2Int, int) next, (Vector2Int, int) current)> blacklist = new();
                var paths = new PlannedPath[pathCount];
                for (int i = 0; i < pathLengths.Length; i++)
                {
                    PlannedPath path = new(pathLengths[i], startPos, starts[i], ref nodes, ref blacklist, random.NewSeed());
                    paths[i] = path;
                    queue.Add(path, 1);
                }
                foreach (var path in paths)
                {
                    WaitForStep(StepType.Step);
                    float newWeight = path.Step()!.Value;
                    queue.UpdateWeight(path, newWeight);
                }
                int steps = 0;
                while (queue.Count > 0 && steps < stepsUntilFail)
                {
                    WaitForStep(StepType.Step);
                    PlannedPath path = queue.PopRandom();
                    float? newWeight = path.Step();
                    if (newWeight.HasValue)
                        queue.Add(path, newWeight.Value);
                    steps++;
                    RegisterGizmos(StepType.Step, () => DrawPaths(paths));
                }
                if (queue.Count == 0)
                {
                    foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                        retNodes[v.x + v.y * WorldUtils.WORLD_SIZE.x] = nodes[v.x, v.y];
                    Debug.Log($"Picked Paths in {steps} steps");
                    RegisterGizmos(StepType.Phase, () => DrawPaths(paths));
                }
                else
                {
                    failed[0] = true;
                }
            }

            static IEnumerable<GizmoManager.GizmoObject> DrawPaths(IEnumerable<PlannedPath> paths)
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach (var path in paths)
                {
                    Vector3? prevPos = null;
                    foreach (var node in path.path)
                    {
                        Vector3 pos = WorldUtils.TileToWorldPos((Vector3Int)node.pos);
                        bool isPrev = path.prev is not null && node.pos == path.prev.Value.pos;
                        bool isCurrent = path.next is not null && node.pos == path.next.Value.pos;
                        bool isShort = prevPos is not null && (prevPos.Value - pos).sqrMagnitude < 2;
                        gizmos.Add(new GizmoManager.Cube(isPrev || isCurrent ? Color.cyan : Color.red, pos, 0.3f));
                        if (prevPos is Vector3 pp)
                        {
                            gizmos.Add(new GizmoManager.Line(isShort ? Color.yellow : isCurrent ? Color.cyan : Color.red, pp, pos));
                        }
                        prevPos = pos;
                    }
                }
                return gizmos;
            }
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
                bool[,] pathTiles = new bool[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
                LevelGenTile[] paths = new LevelGenTile[targetCount];
                for (int i = 0; i < targetCount; i++)
                {
                    paths[i] = Tiles[starts[i]];
                    pathTiles[paths[i].pos.x, paths[i].pos.y] = true;
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
