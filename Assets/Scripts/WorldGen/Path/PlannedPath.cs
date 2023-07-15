using Random;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace WorldGen.Path
{
    public class PlannedPath
    {
        readonly int length_;
        readonly int startPos_;
        public readonly LinkedList<(Vector2Int pos, int dist)> path;
        public LinkedListNode<(Vector2Int pos, int dist)> prev;
        public LinkedListNode<(Vector2Int pos, int dist)> next;
        readonly int[,] nodes_;
        readonly HashSet<((Vector2Int, int) prev, (Vector2Int, int) next, (Vector2Int, int) current)> blacklist_;
        readonly Random.Random random_;

        public PlannedPath(int length, int startPos, Vector2Int target, ref int[,] nodes, ref HashSet<((Vector2Int, int) prev, (Vector2Int, int) next, (Vector2Int, int) current)> blacklist, ulong randomSeed)
        {
            length_ = length;
            startPos_ = startPos;
            nodes_ = nodes;
            blacklist_ = blacklist;
            path = new();
            Vector2Int start = GetOffset(WorldUtils.ORIGIN, target, startPos);
            path.AddFirst((start, startPos));
            nodes[start.x, start.y] = startPos;
            path.AddLast((target, length));
            nodes[target.x, target.y] = length_;
            random_ = new(randomSeed);
        }

        Vector2Int GetOffset(Vector2Int origin, Vector2Int target, int magnitude)
        {
            return origin + WorldUtils.GetMainDir(origin, target, random_) * magnitude;
        }

        bool IsUnreachable(Vector2Int pos)
        {
            return pos.x < 0 || pos.y < 0 || pos.x >= WorldUtils.WORLD_SIZE.x || pos.y >= WorldUtils.WORLD_SIZE.y || nodes_[pos.x, pos.y] != int.MaxValue;
        }

        public float? Step()
        {
            if (path.Count == length_ - startPos_ + 1)
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
                    dist = random_.Int(prev.Value.dist + 2, next.Value.dist - 1);
                else
                    dist = random_.Int(prev.Value.dist + 1, next.Value.dist);
                int distPrev = dist - prev.Value.dist;
                int distNext = next.Value.dist - dist;
                var reachableFromPrev = FindReachable(prev.Value.pos, distPrev);
                var reachableFromNext = FindReachable(next.Value.pos, distNext);
                reachableFromNext.IntersectWith(reachableFromPrev);
                RandomSet<Vector2Int> primary = new(random_.NewSeed());
                RandomSet<Vector2Int> secondary = new(random_.NewSeed());
                foreach (Vector2Int p in reachableFromNext)
                {
                    if (blacklist_.Contains((prev.Value, next.Value, (p, dist))))
                        continue;
                    bool isPrimary = WorldUtils.ADJACENT_DIRS.All(n => !IsUnreachable(p + n));
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
                        nodes_[next.Previous!.Value.pos.x, next.Previous.Value.pos.y] = int.MaxValue;
                        path.Remove(next.Previous);
                    }
                    if (next != path.Last)
                    {
                        next = next.Next;
                        nodes_[prev!.Next!.Value.pos.x, prev.Next.Value.pos.y] = int.MaxValue;
                        path.Remove(prev.Next);
                    }
                }
                else
                {
                    blacklist_.Add((prev.Value, next.Value, newNode.Value));
                    path.AddAfter(prev, newNode.Value);
                    nodes_[newNode.Value.pos.x, newNode.Value.pos.y] = newNode.Value.dist;
                    prev = next;
                    next = next.Next;
                }
            }
            while (next is not null && prev is not null && prev.Value.dist + 1 >= next.Value.dist)
            {
                prev = next;
                next = next.Next;
            }

            return 1 - (float)path.Count / (length_ + 1);
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
                if (dist >= maxDist)
                    continue;

                for (int i = 0; i < 4; i++)
                {
                    Vector2Int n = pos + WorldUtils.CARDINAL_DIRS[i];
                    if (found.Contains(n) || IsUnreachable(n))
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
            return ret;
        }
    }
}
