using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.LevelGenerator;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Path
{
    public class PathPlanner : MonoBehaviour
    {
        [SerializeField] int[] targetLengths;
        [SerializeField] int stepsUntilFail;

        public (JobDataInterface jobData, Vector2Int[] targets) PickTargets()
        {
            Vector2Int[] ret = new Vector2Int[targetLengths.Length];
            JobDataInterface jobData = new(Allocator.Persistent);
            JobHandle handle = new PickTargetsJob
            {
                targetLengths = jobData.Register(targetLengths, false),
                picked = jobData.Register(ret, true)
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return (jobData, ret);
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
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
                    {
                        if (x == 0 || y == 0 || x == WorldUtils.WORLD_SIZE.x - 1 || y == WorldUtils.WORLD_SIZE.y - 1)
                        {
                            if ((x + y - WorldUtils.ORIGIN.x - WorldUtils.ORIGIN.y) % 2 == 0)
                                evenTargets.Add(new(x, y));
                            else
                                oddTargets.Add(new(x, y));
                        }
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

        public (JobDataInterface jobData, PlannedPath[] paths) PickPaths(Vector2Int[] targets)
        {
            JobDataInterface jobData = new(Allocator.Persistent);
            JobHandle handle = new PickPathsJob
            {
                targetLengths = jobData.Register(targetLengths, false),
                targets = jobData.Register(targets, false),
                stepsUntilFail = stepsUntilFail
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return (jobData, null);
        }

        struct PickPathsJob : IJob
        {
            public NativeArray<int> targetLengths;
            public NativeArray<Vector2Int> targets;
            public int stepsUntilFail;
            public void Execute()
            {
                Debug.Log("Picking Paths");

                int pathCount = targetLengths.Length;
                int[,] nodes = new int[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
                    {
                        nodes[x, y] = int.MaxValue;
                    }
                }
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
                while (queue.Count > 0 || steps >= stepsUntilFail)
                {
                    WaitForStep(StepType.Step);
                    PlannedPath path = queue.PopRandom();
                    float? newWeight = path.Step();
                    if (newWeight.HasValue)
                        queue.Add(path, newWeight.Value);
                    steps++;
                    RegisterGizmos(StepType.Step, () => DrawPaths(paths));
                }
                //return if finished
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
                        bool isPrev = path.prev != null && node.pos == path.prev.Value.pos;
                        bool isCurrent = path.next != null && node.pos == path.next.Value.pos;
                        bool isShort = prevPos != null && (prevPos.Value - pos).sqrMagnitude < 2;
                        gizmos.Add(new GizmoManager.Cube(isPrev || isCurrent ? Color.cyan : Color.red, pos, 0.3f));
                        if (prevPos != null)
                        {
                            gizmos.Add(new GizmoManager.Line(isShort ? Color.yellow : isCurrent ? Color.cyan : Color.red, prevPos.Value, pos));
                        }
                        prevPos = pos;
                    }
                }
                return gizmos;
            }
        }

    }
}
