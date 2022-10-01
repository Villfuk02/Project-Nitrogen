using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.LevelGenerator;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Blockers
{
    public class BlockerGenerator : MonoBehaviour
    {
        [SerializeField] Blocker[] blockerSetup;
        public static Blocker[] ALL_BLOCKERS;
        readonly static ThreadSafeRandom random = new();
        public void Prepare()
        {
            ALL_BLOCKERS = blockerSetup;
        }

        public (JobDataInterface jobData, List<Vector2Int> positions, List<int> blockers) PlaceBlockers(Vector2Int[] targets, int[] lengths)
        {
            List<Vector2Int> positions = new();
            List<int> blockers = new();
            JobDataInterface jobData = new(Allocator.Persistent);
            JobHandle handle = new PlaceBlockersJob
            {
                pathTargets = jobData.Register(targets, false),
                pathLengths = jobData.Register(lengths, false),
                positions = jobData.Register(positions, true),
                blockers = jobData.Register(blockers, true)
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return (jobData, positions, blockers);
        }

        struct PlaceBlockersJob : IJob
        {
            public NativeArray<Vector2Int> pathTargets;
            public NativeArray<int> pathLengths;
            public NativeList<Vector2Int> positions;
            public NativeList<int> blockers;
            public void Execute()
            {
                /*
                Scatterer.Scatterer s = gameObject.GetComponent<Scatterer.Scatterer>();
                for (int i = 0; i < ALL_BLOCKERS.Length; i++)
                {
                    if (ALL_BLOCKERS[i].copyModules != "")
                    {
                        Scatterer.ScattererObjectModule[] original = ALL_BLOCKERS.First(m => m.name == ALL_BLOCKERS[i].copyModules).scattererModules;
                        ALL_BLOCKERS[i].scattererModules = new Scatterer.ScattererObjectModule[original.Length];
                        for (int j = 0; j < original.Length; j++)
                        {
                            ALL_BLOCKERS[i].scattererModules[j] = original[j].Clone();
                        }
                    }
                }
                */
                WaitForStep(StepType.Phase);
                Debug.Log("Picking Blockers");

                List<Vector2Int> highlights = new(pathTargets.ToArray())
                {
                    WorldUtils.ORIGIN
                };
                RegisterGizmos(StepType.Phase, () => highlights.Select((p) => new GizmoManager.Cube(Color.yellow, WorldUtils.TileToWorldPos(p), 0.5f)));

                Dictionary<int, float[,]> weightFields = new();
                int maxLayer = 0;
                Dictionary<int, List<int>> layers = new();

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
                        /*
                        if (ALL_BLOCKERS[i].scattererModules.Length > 0)
                        {
                            bool[,] vt = new bool[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
                            for (int j = 0; j < ALL_BLOCKERS[i].scattererModules.Length; j++)
                            {
                                ALL_BLOCKERS[i].scattererModules[j].validTiles = vt;
                                s.SCATTERER_MODULES.Add(ALL_BLOCKERS[i].scattererModules[j]);
                            }
                        }
                        */
                    }
                }
                RandomSet<Vector2Int> emptyTiles = new();
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
                    {
                        Vector2Int v = new(x, y);
                        if (Tiles[v].dist == int.MaxValue)
                            emptyTiles.Add(v);
                    }
                }

                int layer = 0;
                RandomSet<Vector2Int> tilesLeft = new(emptyTiles);
                while (layer <= maxLayer)
                {
                    if (!layers.ContainsKey(layer) || layers[layer].Count == 0)
                    {
                        tilesLeft = new(emptyTiles);
                        layer++;
                        RegisterGizmosIfExactly(StepType.Step, () => DoGizmos(tilesLeft));
                        WaitForStep(StepType.Step);
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
                            RegisterGizmosIfExactly(StepType.Step, () => DoGizmos(tilesLeft));
                            WaitForStep(StepType.Step);
                        }
                        else
                        {
                            RegisterGizmosIfExactly(StepType.Substep, () => DoGizmos(tilesLeft));
                            WaitForStep(StepType.Substep);
                            Vector2Int p = tilesLeft.PopRandom();
                            int? b = TryPlace(p, layers[layer], emptyTiles, weightFields, false);
                            if (b.HasValue)
                            {
                                positions.Add(p);
                                blockers.Add(b.Value);
                            }
                        }
                    }
                }
                RegisterGizmosIfExactly(StepType.Step, () => DoGizmos(tilesLeft));
                WaitForStep(StepType.Step);
                layer = -1;
                foreach (var pos in tilesLeft.AllEntries)
                {
                    Tiles[pos].passable = false;
                }
                while (tilesLeft.Count > 0)
                {
                    RegisterGizmosIfExactly(StepType.Substep, () => DoGizmos(tilesLeft));
                    WaitForStep(StepType.Substep);
                    Vector2Int pos = tilesLeft.PopRandom();
                    Tiles[pos].passable = true;
                    Tiles.RecalculatePaths();
                    bool valid = true;
                    for (int i = 0; i < pathTargets.Length; i++)
                    {
                        Vector2Int t = pathTargets[i];
                        if (Tiles[t].dist != pathLengths[i])
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (!valid)
                    {
                        positions.Add(pos);
                        blockers.Add(TryPlace(pos, layers[layer], emptyTiles, weightFields, true).Value);
                        Tiles[pos].passable = false;
                    }
                }
                Tiles.RecalculatePaths();
                RegisterGizmos(StepType.Phase, () => DoGizmos(tilesLeft));
                Debug.Log("Blockers Picked");
            }

            int? TryPlace(Vector2Int pos, List<int> available, RandomSet<Vector2Int> emptyTiles, Dictionary<int, float[,]> weightFields, bool force)
            {
                List<float> probabilities = new();
                List<bool> placed = new();
                int placedCount = 0;
                for (int i = 0; i < available.Count; i++)
                {
                    Blocker b = ALL_BLOCKERS[available[i]];
                    float probability = b.baseProbability;
                    bool valid = true;
                    if (!b.onSlants && Tiles[pos].slant != WorldUtils.Slant.None)
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
                    bool p = valid && (force || random.NextFloat() < probability);
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
                            int b = available[i];
                            Place(pos, b, available, emptyTiles, weightFields);
                            return b;
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
                                Place(pos, available[i], available, emptyTiles, weightFields);
                                return available[i];
                            }
                        }
                    }
                }
                return null;
            }
            void Place(Vector2Int pos, int blocker, List<int> available, RandomSet<Vector2Int> emptyTiles, Dictionary<int, float[,]> weightFields)
            {
                emptyTiles.Remove(pos);
                Tiles[pos].passable = false;
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
                /*
                if (ALL_BLOCKERS[blocker].scattererModules.Length > 0)
                {
                    ALL_BLOCKERS[blocker].scattererModules[0].validTiles[pos.x, pos.y] = true;
                }*/
                if (ALL_BLOCKERS[blocker].placed >= ALL_BLOCKERS[blocker].max)
                {
                    available.Remove(blocker);
                }
            }

            static List<GizmoManager.GizmoObject> DoGizmos(RandomSet<Vector2Int> tilesLeft)
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach (var pos in tilesLeft.AllEntries)
                {
                    gizmos.Add(new GizmoManager.Cube(Color.cyan, WorldUtils.TileToWorldPos((Vector3Int)pos), 0.3f));
                }
                foreach (var tile in Tiles)
                {
                    Vector3 pos = WorldUtils.TileToWorldPos((Vector3Int)tile.pos);
                    Color c = !tile.passable ? Color.magenta : (tile.dist == int.MaxValue ? Color.red : Color.green);
                    gizmos.Add(new GizmoManager.Cube(c, pos, 0.2f));
                    foreach (var n in tile.neighbors)
                    {
                        if (n != null)
                        {
                            Vector3 other = WorldUtils.TileToWorldPos((Vector3Int)n.pos);
                            gizmos.Add(new GizmoManager.Line(c, pos, Vector3.Lerp(pos, other, 0.5f)));
                        }
                    }
                }
                return gizmos;
            }
        }
    }
}
