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
        /*
        [SerializeField] int stepsUntilFail;
        [SerializeField] int steps;
        [SerializeField] float minDist;
        [SerializeField] int startPos;
        [SerializeField] bool allowOneSteps;
        [SerializeField] int[,] nodes;
        [SerializeField] HashSet<((Vector2Int pos, int dist) prev, (Vector2Int pos, int dist) next, (Vector2Int pos, int dist) me)> blacklist = new();
        [SerializeField] WeightedRandomSet<PathGeneratorPath> queue = new();
        [SerializeField] List<PathGeneratorPath> done = new();
        [SerializeField] bool started, stopped;

        public void Init()
        {
            nodes = new int[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
            for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
            {
                for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
                {
                    nodes[x, y] = int.MaxValue;
                }
            }
            int startPos = targetCount <= 4 ? 1 : 2;

            nodes[origin.x, origin.y] = 0;
            if (startPos > 1)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int p = origin + WorldUtils.CARDINAL_DIRS[i];
                    nodes[p.x, p.y] = 1;
                }
            }
            for (int i = 0; i < targetLengths.Length; i++)
            {
                new PathGeneratorPath(targetLengths[i], startPos);
            }
            StartCoroutine(FindPath());
        }

        IEnumerator FindPath()
        {
            foreach ((var path, var _) in queue.AllEntries)
            {
                path.Step(false, allowOneSteps);
            }
            while (queue.Count > 0)
            {
                queue.PopRandom().Step(true, allowOneSteps);
                steps++;
                if (steps >= stepsUntilFail)
                {
                    oddTargets.Clear();
                    evenTargets.Clear();
                    chosenTargets.Clear();
                    blacklist.Clear();
                    queue.Clear();
                    done.Clear();
                    steps = 0;
                    Init();
                    yield break;
                }
            }
            yield return null;
            stopped = true;
        }

        public static HashSet<Vector2Int> FindReachable(Vector2Int startPos, int maxDist)
        {
            HashSet<Vector2Int> ret = new();
            HashSet<Vector2Int> found = new();
            PriorityQueue<Vector2Int, int> queue = new();
            queue.Enqueue(startPos, 0);
            while (queue.Count > 0)
            {
                queue.TryDequeue(out Vector2Int pos, out int dist);
                found.Add(pos);
                if ((dist + maxDist) % 2 == 0)
                    ret.Add(pos);
                if (dist < maxDist)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2Int n = pos + WorldUtils.CARDINAL_DIRS[i];
                        if (found.Contains(n) || n.x < 0 || n.y < 0 || n.x >= WorldUtils.WORLD_SIZE.x || n.y >= WorldUtils.WORLD_SIZE.y || nodes[n.x, n.y] != int.MaxValue)
                            continue;
                        if (queue.Contains(n))
                        {
                            if (queue.PeekPriority(n) > dist + 1)
                            {
                                queue.ChangePriority(n, dist + 1);
                            }
                        }
                        else
                        {
                            queue.Enqueue(n, dist + 1);
                        }
                    }
                }
            }
            return ret;
        }

        private void OnDrawGizmos()
        {
            if (started && !stopped)
            {
                foreach (var (path, weight) in queue.AllEntries)
                {
                    Vector3? prevPos = null;
                    foreach (var node in path.path)
                    {
                        Vector3 pos = WorldUtils.TileToWorldPos((Vector3Int)node.pos);
                        Handles.Label(pos, node.dist.ToString());
                        bool isPrev = path.prev != null && node.pos == path.prev.Value.pos;
                        bool isCurrent = path.next != null && node.pos == path.next.Value.pos;
                        bool isShort = prevPos != null && (prevPos.Value - pos).sqrMagnitude < 2;
                        Gizmos.color = isPrev || isCurrent ? Color.cyan : Color.red;
                        Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
                        if (prevPos != null)
                        {
                            Gizmos.color = isShort ? Color.yellow : isCurrent ? Color.cyan : Color.red;
                            Gizmos.DrawLine(prevPos.Value, pos);
                        }
                        prevPos = pos;
                    }
                }

                Gizmos.color = Color.magenta;
                foreach (var path in done)
                {
                    Vector3? prevPos = null;
                    foreach (var node in path.path)
                    {
                        Vector3 pos = WorldUtils.TileToWorldPos((Vector3Int)node.pos);
                        Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
                        if (prevPos != null)
                        {
                            Gizmos.DrawLine(prevPos.Value, pos);
                        }
                        prevPos = pos;
                    }
                }
                Gizmos.color = Color.magenta;
                if (origin.x > 0)
                {
                    Gizmos.DrawWireCube(WorldUtils.TileToWorldPos((Vector3Int)origin), Vector3.one * 0.5f);
                }
            }
        }*/

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
        public (JobDataInterface jobInt, Vector2Int[] targets) PickTargets()
        {
            Vector2Int[] ret = new Vector2Int[targetLengths.Length];
            JobDataInterface jobInt = new(Allocator.Persistent);
            JobHandle handle = new PickTargetsJob
            {
                targetLengths = jobInt.Register(targetLengths, false),
                picked = jobInt.Register(ret, true)
            }.Schedule();
            jobInt.RegisterHandle(this, handle);
            return (jobInt, ret);
        }
    }
}
