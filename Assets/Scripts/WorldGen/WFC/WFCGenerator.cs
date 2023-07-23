using Data.LevelGen;
using Random;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Utils;
using static Utils.TimingUtils;
using static WorldGen.WorldGenerator;

namespace WorldGen.WFC
{
    public class WFCGenerator : MonoBehaviour
    {
        [SerializeField] int backupDepth;
        public static TerrainType Terrain { get; private set; }
        public static Random.Random Random { get; private set; }
        public static float MaxEntropy { get; private set; }

        public void Init(TerrainType terrainType, ulong randomSeed)
        {
            Terrain = terrainType;
            Random = new(randomSeed);
            MaxEntropy = CalculateEntropy(terrainType.Modules.GroupBy(m => m.Weight).ToDictionary(g => g.Key, g => g.Count() * (terrainType.MaxHeight + 1)));
        }

        public JobDataInterface Generate(Vector2Int[][] paths, out Array2D<int> modules, out Array2D<int> heights)
        {
            Vector2Int slotWorldSize = WorldUtils.WORLD_SIZE + Vector2Int.one;

            var flatModules = new int[slotWorldSize.x * slotWorldSize.y];
            modules = new(flatModules, slotWorldSize);

            var flatHeights = new int[slotWorldSize.x * slotWorldSize.y];
            heights = new(flatHeights, slotWorldSize);

            int[] flatPathDistances = new int[WorldUtils.WORLD_SIZE.x * WorldUtils.WORLD_SIZE.y];
            Array.Fill(flatPathDistances, int.MaxValue);
            Array2D<int> distances = new(flatPathDistances, WorldUtils.WORLD_SIZE);
            foreach (var path in paths)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    distances[path[i]] = path.Length - i;
                }
            }

            JobDataInterface jobData = new(Allocator.Persistent);
            JobHandle handle = new GenerateJob
            {
                flatPathDistances = jobData.Register(flatPathDistances, JobDataInterface.Mode.Input),
                flatModules = jobData.Register(flatModules, JobDataInterface.Mode.Output),
                flatHeights = jobData.Register(flatHeights, JobDataInterface.Mode.Output),
                failed = jobData.RegisterFailed(),
                backupDepth = backupDepth,
                randomSeed = Random.NewSeed()
            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }

        struct GenerateJob : IJob
        {
            public NativeArray<int> flatPathDistances;
            public NativeArray<int> flatModules;
            public NativeArray<int> flatHeights;
            public NativeArray<bool> failed;
            public int backupDepth;
            public ulong randomSeed;
            public void Execute()
            {
                WaitForStep(StepType.Phase);
                Debug.Log("Starting WFC");

                Array2D<int> pathDistances = new(flatPathDistances.ToArray(), WorldUtils.WORLD_SIZE);
                (var dirty, WFCState state) = InitWFC(pathDistances, randomSeed);

                FixedCapacityStack<WFCState> stateStack = new(backupDepth);
                int steps = 0;
                while (state.uncollapsed > 0)
                {
                    while (dirty.Count > 0)
                    {
                        WaitForStep(StepType.MicroStep);
                        UpdateNext(ref state, dirty, stateStack);
                        if (state is null)
                        {
                            Debug.Log("WFC failed");
                            failed[0] = true;
                            return;
                        }
                        RegisterGizmos(StepType.MicroStep, () => DrawEntropy(state, dirty));
                    }
                    if (steps == 0)
                        Debug.Log("Initial position solved");
                    WaitForStep(StepType.Step);
                    stateStack.Push(new(state));
                    state.CollapseRandom(dirty);
                    steps++;
                    RegisterGizmosIfExactly(StepType.Step, () => DrawEntropy(state, dirty));
                    RegisterGizmos(StepType.Step, () => DrawMesh(state));
                }

                state.SaveResults(flatModules, flatHeights);

                RegisterGizmos(StepType.Phase, () => DrawMesh(state));
                Debug.Log($"WFC Done in {steps} steps");
            }

            static IEnumerable<GizmoManager.GizmoObject> DrawEntropy(WFCState state, RandomSet<WFCSlot> dirty)
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach ((Vector2Int pos, float weight) in state.entropyQueue.AllEntries)
                {
                    Color c = dirty.Contains(state.slots[pos]) ? Color.red : Color.black;
                    float entropy = MaxEntropy - weight;
                    float size;
                    if (entropy <= MaxEntropy * 0.2f)
                    {
                        size = entropy / (MaxEntropy * 0.2f);
                        c += Color.green;
                    }
                    else
                    {
                        size = entropy / MaxEntropy;
                        c += Color.blue;
                    }
                    gizmos.Add(new GizmoManager.Cube(
                        c,
                        WorldUtils.SlotToWorldPos(pos.x, pos.y),
                        size * 0.6f
                        ));
                }
                return gizmos;
            }

            static IEnumerable<GizmoManager.GizmoObject> DrawMesh(WFCState state)
            {
                return state.slots.Where(s => s.Collapsed is not null)
                    .Select(s => new { s, m = s.Collapsed })
                    .Select(t => new GizmoManager.Mesh(Color.white, t.m.Collision,
                        WorldUtils.SlotToWorldPos(t.s.pos.x, t.s.pos.y, t.s.Height + t.m.HeightOffset),
                        new(t.m.Flipped ? -1 : 1, 1, 1), Quaternion.Euler(0, 90 * t.m.Rotated, 0))).ToList();
            }

            static GizmoManager.Cube DrawPassage(Vector2Int tilePos, int direction, (bool passable, bool unpassable) p)
            {
                Color c = p switch
                {
                    (false, false) => Color.blue,
                    (true, false) => Color.green,
                    (false, true) => Color.red,
                    (true, true) => Color.yellow
                };
                return new(c, WorldUtils.TileToWorldPos(tilePos + 0.5f * (Vector2)WorldUtils.CARDINAL_DIRS[direction]), 0.25f);
            }
            static (RandomSet<WFCSlot> dirty, WFCState state) InitWFC(IReadOnlyArray2D<int> pathDistances, ulong newSeed)
            {
                WFCState state = new();
                RandomSet<WFCSlot> dirty = new(newSeed);
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
                    {
                        WFCSlot s = new(x, y, ref state);
                        state.slots[x, y] = s;
                        dirty.Add(s);
                    }
                }

                WFCTile centerTile = state.GetTileAt(WorldUtils.ORIGIN);
                centerTile.slants.Clear();
                centerTile.slants.Add(WorldUtils.Slant.None);

                foreach ((var pos, int distance) in pathDistances.IndexedEnumerable)
                {
                    if (distance == int.MaxValue)
                        continue;

                    for (int i = 0; i < 4; i++)
                    {
                        Vector2Int neighbor = pos + WorldUtils.CARDINAL_DIRS[i];
                        if (!pathDistances.TryGet(neighbor, out var neighborDistance) || neighborDistance - distance == 1 || neighborDistance - distance == -1)
                        {
                            RegisterGizmos(StepType.Step, () => DrawPassage(pos, i, (true, false)));
                            state.SetValidPassageAtTile(pos, i, (true, false));
                        }
                        else if (neighborDistance != int.MaxValue)
                        {
                            RegisterGizmos(StepType.Step, () => DrawPassage(pos, i, (false, true)));
                            state.SetValidPassageAtTile(pos, i, (false, true));
                        }
                    }
                }
                return (dirty, state);
            }

            static void UpdateNext(ref WFCState? state, RandomSet<WFCSlot> dirty, FixedCapacityStack<WFCState> stateStack)
            {
                WFCSlot s = dirty.PopRandom();
                (WFCSlot n, bool backtrack) = s.UpdateValidModules(state);
                if (backtrack)
                {
                    Backtrack(ref state, dirty, stateStack);
                }
                else if (n is not null)
                {
                    MarkNeighborsDirty(n.pos, n.UpdateConstraints(state), state, dirty);
                    state!.slots[n.pos] = n;
                }
            }
            static void Backtrack(ref WFCState? state, RandomSet<WFCSlot> dirty, FixedCapacityStack<WFCState> stateStack)
            {
                //Debug.Log("Backtracking");
                dirty.Clear();
                Vector2Int lastCollapsedSlot = state!.lastCollapsedSlot;
                (Module module, int height) lastCollapsedTo = state.lastCollapsedTo;
                if (stateStack.Count == 0)
                {
                    state = null;
                }
                else
                {
                    state = stateStack.Pop();
                    state.slots[lastCollapsedSlot].MarkInvalid(lastCollapsedTo);
                    MarkDirty(lastCollapsedSlot.x, lastCollapsedSlot.y, state, dirty);
                }
            }
        }

        public static void MarkNeighborsDirty(Vector2Int pos, IEnumerable<Vector2Int> offsets, in WFCState state, RandomSet<WFCSlot> dirty)
        {
            foreach (var offset in offsets)
                MarkDirty(pos.x + offset.x, pos.y + offset.y, state, dirty);
        }
        public static void MarkDirty(int x, int y, in WFCState state, RandomSet<WFCSlot> dirty)
        {
            if (!state.slots.TryGet(new(x, y), out var s) || s.Collapsed is not null)
                return;
            dirty.TryAdd(s);
        }


        public static float CalculateEntropy(Dictionary<float, int> weights)
        {
            float totalWeight = weights.Sum(w => w.Value * w.Key);
            float totalEntropy = 0;
            foreach (var w in weights)
            {
                float probability = w.Key / totalWeight;
                totalEntropy -= probability * w.Value * Mathf.Log(probability, 2);
            }
            return totalEntropy;
        }
    }
}
