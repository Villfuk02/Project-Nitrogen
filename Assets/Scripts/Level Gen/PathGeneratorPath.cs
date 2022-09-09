using System.Collections.Generic;
using UnityEngine;

public class PathGeneratorPath
{
    readonly int targetLength;
    readonly int startPos;
    public readonly LinkedList<(Vector2Int pos, int dist)> path;
    public LinkedListNode<(Vector2Int pos, int dist)> prev;
    public LinkedListNode<(Vector2Int pos, int dist)> next;

    public PathGeneratorPath(int length, int startPos)
    {
        targetLength = length;
        Vector2Int target = PathGenerator.ChooseTarget(length);
        path = new();
        Vector2Int start = GetOffset(PathGenerator.origin, target, startPos);
        path.AddFirst((start, startPos));
        PathGenerator.nodes[start.x, start.y] = startPos;
        path.AddLast((target, targetLength));
        PathGenerator.nodes[target.x, target.y] = targetLength;
        PathGenerator.queue.Add(this, 1);
        this.startPos = startPos;
    }

    static Vector2Int GetOffset(Vector2Int origin, Vector2Int target, int magnitude)
    {
        return origin + WorldUtils.GetMainDir(origin, target) * magnitude;
    }

    public void Step(bool reAdd, bool allowOneSteps)
    {
        if (path.Count == targetLength - startPos + 1)
        {
            PathGenerator.done.Add(this);
            return;
        }

        if (next == null)
        {
            prev = path.First;
            next = prev.Next;
        }
        else
        {
            int dist;
            if (!allowOneSteps && prev.Value.dist + 3 < next.Value.dist)
                dist = Random.Range(prev.Value.dist + 2, next.Value.dist - 1);
            else
                dist = Random.Range(prev.Value.dist + 1, next.Value.dist);
            int distPrev = dist - prev.Value.dist;
            int distNext = next.Value.dist - dist;
            HashSet<Vector2Int> reachableFromPrev = PathGenerator.FindReachable(prev.Value.pos, distPrev);
            HashSet<Vector2Int> reachableFromNext = PathGenerator.FindReachable(next.Value.pos, distNext);
            reachableFromNext.IntersectWith(reachableFromPrev);
            RandomSet<Vector2Int> primary = new();
            RandomSet<Vector2Int> secondary = new();
            foreach (Vector2Int p in reachableFromNext)
            {
                if (PathGenerator.blacklist.Contains((prev.Value, next.Value, (p, dist))))
                    continue;
                bool isPrimary = true;
                for (int i = 0; i < 8; i++)
                {
                    Vector2Int n = p + WorldUtils.ADJACENT_DIRS[i];
                    if (n.x < 0 || n.y < 0 || n.x >= WorldUtils.WORLD_SIZE.x || n.y >= WorldUtils.WORLD_SIZE.y || PathGenerator.nodes[n.x, n.y] != int.MaxValue)
                    {
                        isPrimary = false;
                        break;
                    }
                }
                if (isPrimary)
                    primary.Add(p);
                else
                    secondary.Add(p);
            }
            (Vector2Int pos, int dist)? newNode = null;
            if (primary.Count > 0)
            {
                newNode = (primary.PopRandom(), dist);
            }
            else if (secondary.Count > 0 && (distPrev == 1 || distNext == 1))
            {
                newNode = (secondary.PopRandom(), dist);
            }
            if (newNode == null)
            {
                if (prev != path.First)
                {
                    prev = prev.Previous;
                    PathGenerator.nodes[next.Previous.Value.pos.x, next.Previous.Value.pos.y] = int.MaxValue;
                    path.Remove(next.Previous);
                }
                if (next != path.Last)
                {
                    next = next.Next;
                    PathGenerator.nodes[prev.Next.Value.pos.x, prev.Next.Value.pos.y] = int.MaxValue;
                    path.Remove(prev.Next);
                }
            }
            else
            {
                PathGenerator.blacklist.Add((prev.Value, next.Value, newNode.Value));
                path.AddAfter(prev, newNode.Value);
                PathGenerator.nodes[newNode.Value.pos.x, newNode.Value.pos.y] = newNode.Value.dist;
                prev = next;
                next = next.Next;
            }
        }
        while (next != null && prev != null && prev.Value.dist + 1 >= next.Value.dist)
        {
            prev = next;
            next = next.Next;
        }

        if (reAdd)
            PathGenerator.queue.Add(this, 1 - ((float)path.Count / (targetLength + 1)));
    }
}
