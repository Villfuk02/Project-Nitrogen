using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockerGenerator : LevelGeneratorPart
{
    public Blocker[] ALL_BLOCKERS;
    public int layer = 0;
    public int maxLayer = 0;
    public Dictionary<int, List<int>> layers = new();
    public RandomSet<Vector2Int> emptyTiles = new();
    public RandomSet<Vector2Int> tilesLeft = new();
    public Dictionary<int, float[,]> weightFields = new();
    public static Node[,] graph;

    public class Node
    {
        public Vector2Int pos;
        public bool passable;
        public int dist;
        public Node[] connections;

        public Node(Vector2Int pos)
        {
            passable = false;
            dist = int.MaxValue;
            this.pos = pos;
        }
    }

    public override void Init()
    {
        Scatterer s = gameObject.GetComponent<Scatterer>();
        for (int i = 0; i < ALL_BLOCKERS.Length; i++)
        {
            if (ALL_BLOCKERS[i].copyModules != "")
            {
                ScattererObjectModule[] original = ALL_BLOCKERS.First(m => m.name == ALL_BLOCKERS[i].copyModules).scattererModules;
                ALL_BLOCKERS[i].scattererModules = new ScattererObjectModule[original.Length];
                for (int j = 0; j < original.Length; j++)
                {
                    ALL_BLOCKERS[i].scattererModules[j] = original[j].Clone();
                }
            }
        }
        for (int i = 0; i < ALL_BLOCKERS.Length; i++)
        {
            if (ALL_BLOCKERS[i].enabled)
            {
                int l = ALL_BLOCKERS[i].layer;
                if (maxLayer < l)
                    maxLayer = l;
                if (!layers.ContainsKey(l))
                {
                    layers[l] = new();
                }
                layers[l].Add(i);

                if (ALL_BLOCKERS[i].scattererModules.Length > 0)
                {
                    bool[,] vt = new bool[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
                    for (int j = 0; j < ALL_BLOCKERS[i].scattererModules.Length; j++)
                    {
                        ALL_BLOCKERS[i].scattererModules[j].validTiles = vt;
                        s.SCATTERER_MODULES.Add(ALL_BLOCKERS[i].scattererModules[j]);
                    }
                }
            }
        }
        graph = new Node[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
        for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
        {
            for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
            {
                emptyTiles.Add(new(x, y));
                graph[x, y] = new(new(x, y));
            }
        }
        for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
        {
            for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
            {
                List<Node> connections = new();
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int p = new Vector2Int(x, y) + WorldUtils.CARDINAL_DIRS[i];
                    Vector2Int pp = (new Vector2Int(2 * x + 1, 2 * y + 1) + WorldUtils.CARDINAL_DIRS[i]) / 2;
                    if (p.x >= 0 && p.y >= 0 && p.x < WorldUtils.WORLD_SIZE.x && p.y < WorldUtils.WORLD_SIZE.y && WFCGenerator.state.GetValidPassagesAt(pp.x, pp.y, i % 2 == 1).passable)
                    {
                        connections.Add(graph[p.x, p.y]);
                    }
                }
                graph[x, y].connections = connections.ToArray();
            }
        }
        emptyTiles.Remove(PathGenerator.origin);
        graph[PathGenerator.origin.x, PathGenerator.origin.y].passable = true;
        foreach (var path in PathGenerator.done)
        {
            foreach ((Vector2Int p, int _) in path.path)
            {
                emptyTiles.TryRemove(p);
                graph[p.x, p.y].passable = true;
            }
        }
        StartCoroutine(GenerateBlockers());
    }

    IEnumerator GenerateBlockers()
    {
        tilesLeft = new(emptyTiles);
        while (layer <= maxLayer)
        {
            if (!layers.ContainsKey(layer) || layers[layer].Count == 0)
            {
                tilesLeft = new(emptyTiles);
                layer++;
            }
            else
            {
                if (tilesLeft.Count == 0)
                {
                    if (layers[layer].TrueForAll(b => ALL_BLOCKERS[b].placed >= ALL_BLOCKERS[b].min))
                    {
                        layer++;
                    }
                    tilesLeft = new(emptyTiles);
                }
                else
                {
                    TryPlace(tilesLeft.PopRandom(), false);
                }
            }
        }
        layer = -1;
        while (tilesLeft.Count > 0)
        {
            Vector2Int pos = tilesLeft.PopRandom();
            graph[pos.x, pos.y].passable = true;
            RecalculatePaths();
            bool valid = true;
            for (int i = 0; i < PathGenerator.chosenTargets.Count; i++)
            {
                Vector2Int t = PathGenerator.chosenTargets[i];
                if (graph[t.x, t.y].dist != PathGenerator.targetLengths[i])
                {
                    valid = false;
                    break;
                }
            }
            if (!valid)
            {
                TryPlace(pos, true);
                graph[pos.x, pos.y].passable = false;
            }
        }
        RecalculatePaths();
        yield return null;
        stopped = true;
    }

    void TryPlace(Vector2Int pos, bool force)
    {
        List<int> available = layers[layer];
        List<float> probabilities = new();
        List<bool> placed = new();
        int placedCount = 0;
        for (int i = 0; i < available.Count; i++)
        {
            Blocker b = ALL_BLOCKERS[available[i]];
            float probability = b.baseProbability;
            bool valid = true;
            if (!b.onSlants && !WFCGenerator.state.GetTile(pos + Vector2Int.one).slants.Contains(WorldUtils.Slant.None))
            {
                probability = 0;
                valid = false;
            }
            else
            {
                for (int j = 0; j < b.forces.Length; j++)
                {
                    if (b.forces[j] != 0 && weightFields.ContainsKey(j))
                    {
                        probability += b.forces[j] * weightFields[j][pos.x, pos.y];
                    }
                }
            }
            probability = Mathf.Clamp01(probability);
            probabilities.Add(probability);
            bool p = valid && (force || Random.value < probability);
            placed.Add(p);
            if (p)
                placedCount++;
        }
        if (placedCount == 1)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i])
                {
                    Place(pos, available[i]);
                    break;
                }
            }
        }
        else if (placedCount > 1)
        {
            float totalProbability = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i])
                {
                    totalProbability += probabilities[i];
                }
            }
            float r = Random.Range(0, totalProbability);
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i])
                {
                    r -= probabilities[i];
                    if (r <= 0)
                    {
                        Place(pos, available[i]);
                        break;
                    }
                }
            }
        }
    }

    void Place(Vector2Int pos, int blocker)
    {
        emptyTiles.Remove(pos);
        graph[pos.x, pos.y].passable = false;
        if (!weightFields.ContainsKey(blocker))
        {
            weightFields[blocker] = new float[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
        }
        foreach (var p in emptyTiles.AllEntries)
        {
            Vector2Int dist = p - pos;
            weightFields[blocker][p.x, p.y] += 1f / dist.sqrMagnitude;
        }
        ALL_BLOCKERS[blocker].placed++;
        if (ALL_BLOCKERS[blocker].scattererModules.Length > 0)
        {
            ALL_BLOCKERS[blocker].scattererModules[0].validTiles[pos.x, pos.y] = true;
        }
        if (ALL_BLOCKERS[blocker].placed >= ALL_BLOCKERS[blocker].max)
        {
            layers[layer].Remove(blocker);
        }
    }

    void RecalculatePaths()
    {
        foreach (var node in graph)
        {
            node.dist = int.MaxValue;
        }
        PriorityQueue<Vector2Int, int> queue = new();
        queue.Enqueue(PathGenerator.origin, 0);
        while (queue.Count > 0)
        {
            queue.TryDequeue(out Vector2Int pos, out int dist);
            graph[pos.x, pos.y].dist = dist;
            foreach (var n in graph[pos.x, pos.y].connections)
            {
                if (n.dist == int.MaxValue && n.passable)
                {
                    if (queue.Contains(n.pos))
                    {
                        if (queue.PeekPriority(n.pos) > dist + 1)
                        {
                            queue.ChangePriority(n.pos, dist + 1);
                        }
                    }
                    else
                    {
                        queue.Enqueue(n.pos, dist + 1);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (started && !stopped)
        {
            Gizmos.color = Color.cyan;
            foreach (var pos in tilesLeft.AllEntries)
            {
                Gizmos.DrawWireCube(WorldUtils.TileToWorldPos((Vector3Int)pos), Vector3.one * 0.5f);
            }
            /*foreach (var node in graph)
            {
                if (!node.passable)
                    continue;
                Gizmos.color = node.dist == int.MaxValue ? Color.red : Color.green;
                Vector3 pos = WorldUtils.TileToWorldPos((Vector3Int)node.pos);
                Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
                foreach (var n in node.connections)
                {
                    Vector3 other = WorldUtils.TileToWorldPos((Vector3Int)n.pos);
                    Gizmos.DrawLine(Vector3.Lerp(pos, other, 0.2f), Vector3.Lerp(pos, other, 0.5f));
                }
            }*/
        }
    }
}
