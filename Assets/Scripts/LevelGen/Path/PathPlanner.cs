using Assets.Scripts.LevelGen.Utils;
using Assets.Scripts.Utils;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Assets.Scripts.LevelGen.LevelGenerator;

namespace Assets.Scripts.LevelGen.Path
{
    public class PathPlanner : MonoBehaviour
    {
        public int[] targetLengths;
        [SerializeField] int maxPathsPerTarget;
        [SerializeField] int stepsUntilFail;
        [SerializeField] int minMergeDistance;

        public JobDataInterface PickTargets(out Vector2Int[] targets)
        {
            targets = new Vector2Int[targetLengths.Length];
            JobDataInterface jobData = new(Allocator.Persistent);
            JobHandle handle = new PickTargetsJob
            {
                targetLengths = jobData.Register(targetLengths, false),
                picked = jobData.Register(targets, true)
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }

        struct PickTargetsJob : IJob
        {
            public NativeArray<int> targetLengths;
            public NativeArray<Vector2Int> picked;
            public void Execute()
            {
                WaitForStep(StepType.Phase);
                Debug.Log("Picking Targets");

                RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(WorldUtils.ORIGIN), 0.4f));

                WaitForStep(StepType.Step);

                RandomSet<Vector2Int> oddTargets = new();
                RandomSet<Vector2Int> evenTargets = new();
                foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                {
                    if (v.x == 0 || v.y == 0 || v.x == WorldUtils.WORLD_SIZE.x - 1 || v.y == WorldUtils.WORLD_SIZE.y - 1)
                    {
                        if ((v.x + v.y - WorldUtils.ORIGIN.x - WorldUtils.ORIGIN.y) % 2 == 0)
                            evenTargets.Add(v);
                        else
                            oddTargets.Add(v);
                    }
                }
                int targetCount = targetLengths.Length;
                List<Vector2Int> pickedTargets = new();
                float minDist = (WorldUtils.WORLD_SIZE.x + WorldUtils.WORLD_SIZE.y) / targetLengths.Length * 0.9f;
                minDist *= minDist;
                for (int i = 0; i < targetCount; i++)
                {
                    WaitForStep(StepType.Substep);
                    ChooseTarget(targetLengths[i], minDist, targetLengths[i] % 2 == 0 ? evenTargets : oddTargets, pickedTargets);
                    picked[i] = pickedTargets[i];
                    RegisterGizmos(StepType.Phase, () => new GizmoManager.Cube(Color.magenta, WorldUtils.TileToWorldPos(pickedTargets[i]), 0.4f));
                }

            }
            void ChooseTarget(int length, float minDist, RandomSet<Vector2Int> available, List<Vector2Int> picked)
            {
                Vector2Int ret;
                bool valid;
                do
                {
                    ret = available.PopRandom();
                    Vector2Int rel = ret - WorldUtils.ORIGIN;
                    if (Mathf.Abs(rel.x) + Mathf.Abs(rel.y) > length)
                    {
                        valid = false;
                        continue;
                    }
                    valid = true;
                    foreach (Vector2Int t in picked)
                    {
                        if ((ret - t).sqrMagnitude < minDist)
                        {
                            valid = false;
                            break;
                        }
                    }
                } while (!valid);
                picked.Add(ret);
            }
        }

        public JobDataInterface PickPaths(Vector2Int[] targets, out int[] flatNodes)
        {
            flatNodes = new int[WorldUtils.WORLD_SIZE.x * WorldUtils.WORLD_SIZE.y];
            JobDataInterface jobData = new(Allocator.Persistent);
            JobHandle handle = new PickPathsJob
            {
                targetLengths = jobData.Register(targetLengths, false),
                targets = jobData.Register(targets, false),
                retNodes = jobData.Register(flatNodes, true),
                stepsUntilFail = stepsUntilFail,
                failed = jobData.RegisterFailed()
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }

        struct PickPathsJob : IJob
        {
            public NativeArray<int> targetLengths;
            public NativeArray<Vector2Int> targets;
            public NativeArray<int> retNodes;
            public NativeArray<bool> failed;
            public int stepsUntilFail;
            public void Execute()
            {
                Debug.Log("Picking Paths");

                int pathCount = targetLengths.Length;
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
                WeightedRandomSet<PlannedPath> queue = new();
                HashSet<((Vector2Int, int) prev, (Vector2Int, int) next, (Vector2Int, int) current)> blacklist = new();
                PlannedPath[] paths = new PlannedPath[pathCount];
                for (int i = 0; i < targetLengths.Length; i++)
                {
                    PlannedPath path = new(targetLengths[i], startPos, targets[i], ref nodes, ref blacklist);
                    paths[i] = path;
                    queue.Add(path, 1);
                }
                foreach (var path in paths)
                {
                    WaitForStep(StepType.Step);
                    float? newWeight = path.Step();
                    queue.UpdateWeight(path, newWeight.Value);
                }
                int steps = 0;
                List<PlannedPath> done = new();
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

            static List<GizmoManager.GizmoObject> DrawPaths(PlannedPath[] paths)
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
        public JobDataInterface FinalisePaths(Vector2Int[] targets)
        {
            JobDataInterface jobData = new(Allocator.Persistent);

            JobHandle handle = new FinalisePathsJob
            {
                targets = jobData.Register(targets, false),
                maxPathsPerTarget = maxPathsPerTarget,
                minMergeDistance = minMergeDistance
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }
        struct FinalisePathsJob : IJob
        {
            public NativeArray<Vector2Int> targets;
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

                int targetCount = targets.Length;
                bool[,] pathTiles = new bool[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
                LevelGenTile[] paths = new LevelGenTile[targetCount];
                for (int i = 0; i < targetCount; i++)
                {
                    paths[i] = Tiles[targets[i]];
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
    }
}
