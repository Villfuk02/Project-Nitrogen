using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PathGenerator : LevelGeneratorPart
{
    public int stepsUntilFail;
    public int steps;
    public int[] targetLengthsSetup;
    public static int[] targetLengths;
    static float minDist;
    int startPos;
    public bool allowOneSteps;
    public static Vector2Int origin = -Vector2Int.one;
    static readonly RandomSet<Vector2Int> oddTargets = new();
    static readonly RandomSet<Vector2Int> evenTargets = new();
    public static readonly List<Vector2Int> chosenTargets = new();
    public static int[,] nodes;
    public static HashSet<((Vector2Int pos, int dist) prev, (Vector2Int pos, int dist) next, (Vector2Int pos, int dist) me)> blacklist = new();
    public static WeightedRandomSet<PathGeneratorPath> queue = new();
    public static List<PathGeneratorPath> done = new();

    public override void Init()
    {
        targetLengths = targetLengthsSetup;
        origin = (WorldUtils.WORLD_SIZE - Vector2Int.one) / 2;
        nodes = new int[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
        minDist = (WorldUtils.WORLD_SIZE.x + WorldUtils.WORLD_SIZE.y) / targetLengths.Length * 0.9f;
        minDist *= minDist;
        for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
        {
            for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
            {
                nodes[x, y] = int.MaxValue;
                if (x == 0 || y == 0 || x == WorldUtils.WORLD_SIZE.x - 1 || y == WorldUtils.WORLD_SIZE.y - 1)
                {
                    if ((x + y - origin.x - origin.y) % 2 == 0)
                        evenTargets.Add(new(x, y));
                    else
                        oddTargets.Add(new(x, y));
                }
            }
        }
        startPos = targetLengths.Length <= 4 ? 1 : 2;
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

    public static Vector2Int ChooseTarget(int maxDst)
    {
        Vector2Int ret;
        bool valid;
        do
        {
            if (maxDst % 2 == 0)
                ret = evenTargets.PopRandom();
            else
                ret = oddTargets.PopRandom();
            if (Mathf.Abs((ret - origin).x) + Mathf.Abs((ret - origin).y) > maxDst)
            {
                valid = false;
                continue;
            }
            valid = true;
            foreach (Vector2Int t in chosenTargets)
            {
                if ((ret - t).sqrMagnitude < minDist)
                {
                    valid = false;
                    break;
                }
            }
        } while (!valid);
        chosenTargets.Add(ret);
        return ret;
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
                        Gizmos.color = isShort ? Color.yellow : (isCurrent ? Color.cyan : Color.red);
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
    }
}
