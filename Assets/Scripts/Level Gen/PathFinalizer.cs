using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinalizer : LevelGeneratorPart
{
    public int pathsPerStartingPoint;
    public int minMergeDistance;
    public static readonly List<List<Vector2Int>> protoPaths = new();
    public static PathNode[] paths;
    public static bool[,] pathTiles;
    HashSet<Vector2Int>[] forbidden;
    int[] pathsLeft;
    Dictionary<Vector2Int, PathNode> nodeMap = new();

    public class PathNode
    {
        public Vector2Int pos;
        public List<PathNode> next;
        public int index;
        public PathNode(Vector2Int pos, int index)
        {
            this.pos = pos;
            next = new();
            this.index = index;
        }
    }

    public override void Init()
    {
        pathTiles = new bool[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
        forbidden = new HashSet<Vector2Int>[PathGenerator.chosenTargets.Count];
        pathsLeft = new int[PathGenerator.chosenTargets.Count];
        paths = new PathNode[PathGenerator.chosenTargets.Count];
        for (int i = 0; i < PathGenerator.chosenTargets.Count; i++)
        {
            pathsLeft[i] = pathsPerStartingPoint;
            forbidden[i] = new();
            paths[i] = new(PathGenerator.chosenTargets[i], i);
            nodeMap[paths[i].pos] = paths[i];
        }
        StartCoroutine(FinalizePaths());
    }

    IEnumerator FinalizePaths()
    {
        for (int i = 0; i < PathGenerator.chosenTargets.Count; i++)
        {
            Vector2Int t = PathGenerator.chosenTargets[i];
            TracePath(i, BlockerGenerator.graph[t.x, t.y], pathsPerStartingPoint * 7 - 6, new(), 0);
            yield return null;
        }
        ConvertProtoPaths();
        yield return null;
        stopped = true;
    }

    void TracePath(int index, BlockerGenerator.Node n, int pathCount, List<Vector2Int> path, int distToMerge)
    {
        path.Add(n.pos);
        pathTiles[n.pos.x, n.pos.y] = true;
        if (n.dist == 0)
        {
            if (pathsLeft[index] > 0)
            {
                pathsLeft[index]--;
                protoPaths.Add(path);
                foreach (var item in path)
                {
                    forbidden[index].Add(item);
                }
            }
            return;
        }
        distToMerge--;
        int count = 0;
        RandomSet<int> order = new();
        for (int i = 0; i < n.connections.Length; i++)
        {
            if (n.connections[i].dist == n.dist - 1)
            {
                if (distToMerge <= 0 || !forbidden[index].Contains(n.pos))
                {
                    count++;
                    order.Add(i);
                }
            }
        }
        if (count == 0)
        {
            return;
        }
        if (count > 1)
        {
            distToMerge = minMergeDistance;
        }
        int basePathCount = pathCount / count;
        int extra = pathCount % count;
        while (order.Count > 0)
        {
            int c = order.PopRandom();
            if (extra > 0)
            {
                extra--;
                TracePath(index, n.connections[c], basePathCount + 1, new(path), distToMerge);
            }
            else if (basePathCount > 0)
            {
                TracePath(index, n.connections[c], basePathCount, new(path), distToMerge);
            }
        }
    }

    void ConvertProtoPaths()
    {
        foreach (var path in protoPaths)
        {
            int index = nodeMap[path[0]].index;
            PathNode prev = null;
            foreach (var node in path)
            {
                PathNode current;
                if (nodeMap.ContainsKey(node))
                {
                    current = nodeMap[node];
                }
                else
                {
                    current = new(node, index);
                    nodeMap[node] = current;
                }
                if (prev != null && !prev.next.Contains(current))
                {
                    prev.next.Add(current);
                }
                if (current.index != index)
                    break;
                prev = current;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (started && !stopped)
        {
            Gizmos.color = Color.red;
            foreach (var path in protoPaths)
            {
                Vector3? prevPos = null;
                foreach (var node in path)
                {
                    Vector3 pos = WorldUtils.TileToWorldPos((Vector3Int)node);
                    Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
                    if (prevPos != null)
                    {
                        Gizmos.DrawLine(prevPos.Value, pos);
                    }
                    prevPos = pos;
                }
            }
            Gizmos.color = Color.magenta;
            if (paths != null)
            {
                void DrawPath(Vector3? from, PathNode n)
                {
                    Vector3 pos = WorldUtils.TileToWorldPos((Vector3Int)n.pos);
                    Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
                    if (from != null)
                    {
                        Gizmos.DrawLine(from.Value, pos);
                    }
                    foreach (var item in n.next)
                    {
                        DrawPath(pos, item);
                    }
                }
                for (int i = 0; i < paths.Length; i++)
                {
                    DrawPath(null, paths[i]);
                }
            }
        }
    }
}
