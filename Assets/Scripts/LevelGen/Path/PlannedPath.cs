
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace LevelGen.Path
{
    public class PlannedPath
    {
        readonly int length;
        readonly int startPos;
        public readonly LinkedList<(Vector2Int pos, int dist)> path;
        public LinkedListNode<(Vector2Int pos, int dist)> prev;
        public LinkedListNode<(Vector2Int pos, int dist)> next;
        readonly int[,] nodes;
        readonly HashSet<((Vector2Int, int) prev, (Vector2Int, int) next, (Vector2Int, int) current)> blacklist;
        readonly ThreadSafeRandom random;

        public PlannedPath(int length, int startPos, Vector2Int target, ref int[,] nodes, ref HashSet<((Vector2Int, int) prev, (Vector2Int, int) next, (Vector2Int, int) current)> blacklist)
        {
            this.length = length;
            this.startPos = startPos;
            this.nodes = nodes;
            this.blacklist = blacklist;
            path = new();
            Vector2Int start = GetOffset(WorldUtils.ORIGIN, target, startPos);
            path.AddFirst((start, startPos));
            nodes[start.x, start.y] = startPos;
            path.AddLast((target, length));
            nodes[target.x, target.y] = this.length;
            random = new();
        }

        static Vector2Int GetOffset(Vector2Int origin, Vector2Int target, int magnitude)
        {
            return origin + WorldUtils.GetMainDir(origin, target) * magnitude;
        }

        public float? Step()
        {
            if (path.Count == length - startPos + 1)
            {
                return null;
            }

            if (next is null)
            {
                prev = path.First;
                next = prev.Next;
            }
            else
            {
                int dist;
                if (prev.Value.dist + 3 < next.Value.dist)
                    dist = random.Next(prev.Value.dist + 2, next.Value.dist - 1);
                else
                    dist = random.Next(prev.Value.dist + 1, next.Value.dist);
                int distPrev = dist - prev.Value.dist;
                int distNext = next.Value.dist - dist;
                HashSet<Vector2Int> reachableFromPrev = FindReachable(prev.Value.pos, distPrev);
                HashSet<Vector2Int> reachableFromNext = FindReachable(next.Value.pos, distNext);
                reachableFromNext.IntersectWith(reachableFromPrev);
                RandomSet<Vector2Int> primary = new();
                RandomSet<Vector2Int> secondary = new();
                foreach (Vector2Int p in reachableFromNext)
                {
                    if (blacklist.Contains((prev.Value, next.Value, (p, dist))))
                        continue;
                    bool isPrimary = true;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2Int n = p + WorldUtils.ADJACENT_DIRS[i];
                        if (n.x < 0 || n.y < 0 || n.x >= WorldUtils.WORLD_SIZE.x || n.y >= WorldUtils.WORLD_SIZE.y || nodes[n.x, n.y] != int.MaxValue)
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
                if (newNode is null)
                {
                    if (prev != path.First)
                    {
                        prev = prev.Previous;
                        nodes[next.Previous.Value.pos.x, next.Previous.Value.pos.y] = int.MaxValue;
                        path.Remove(next.Previous);
                    }
                    if (next != path.Last)
                    {
                        next = next.Next;
                        nodes[prev.Next.Value.pos.x, prev.Next.Value.pos.y] = int.MaxValue;
                        path.Remove(prev.Next);
                    }
                }
                else
                {
                    blacklist.Add((prev.Value, next.Value, newNode.Value));
                    path.AddAfter(prev, newNode.Value);
                    nodes[newNode.Value.pos.x, newNode.Value.pos.y] = newNode.Value.dist;
                    prev = next;
                    next = next.Next;
                }
            }
            while (next is not null && prev is not null && prev.Value.dist + 1 >= next.Value.dist)
            {
                prev = next;
                next = next.Next;
            }

            return 1 - (float)path.Count / (length + 1);
        }

        HashSet<Vector2Int> FindReachable(Vector2Int startPos, int maxDist)
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
    }
}
