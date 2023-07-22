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

                WaitForStep(StepType.Step);
                (RandomSet<WFCSlot> dirty, WFCState state) = InitWFC(pathDistances, randomSeed);

                FixedCapacityStack<WFCState> stateStack = new(backupDepth);
                while (state.uncollapsed > 0)
                {
                    while (dirty.Count > 0)
                    {
                        WaitForStep(StepType.MicroStep);
                        UpdateNext(ref state, ref dirty, ref stateStack);
                        if (state is null)
                        {
                            Debug.Log("WFC failed");
                            failed[0] = true;
                            return;
                        }
                        RegisterGizmos(StepType.MicroStep, () => DrawEntropy(state, dirty));
                    }
                    WaitForStep(StepType.Step);
                    stateStack.Push(new(state));
                    state.CollapseRandom(ref dirty);
                    RegisterGizmosIfExactly(StepType.Step, () => DrawEntropy(state, dirty));
                    RegisterGizmos(StepType.Step, () => DrawMesh(state));
                }
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
                    {
                        WFCSlot s = state.GetSlot(x, y);
                        flatModules[x + y * (WorldUtils.WORLD_SIZE.x + 1)] = s.Collapsed;
                        flatHeights[x + y * (WorldUtils.WORLD_SIZE.x + 1)] = s.Height;
                    }
                }
                RegisterGizmos(StepType.Phase, () => DrawMesh(state));
                Debug.Log("WFC Done");

            }

            static List<GizmoManager.GizmoObject> DrawEntropy(WFCState state, RandomSet<WFCSlot> dirty)
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach ((Vector2Int pos, float weight) in state.entropyQueue.AllEntries)
                {
                    Color c = dirty.Contains(state.GetSlot(pos.x, pos.y)) ? Color.red : Color.black;
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

            static List<GizmoManager.GizmoObject> DrawMesh(WFCState state)
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
                    {
                        WFCSlot s = state.GetSlot(x, y);
                        if (s.Collapsed != -1)
                        {
                            Module m = Terrain.Modules[s.Collapsed];
                            gizmos.Add(new GizmoManager.Mesh(
                                Color.white,
                                m.Collision,
                                WorldUtils.SlotToWorldPos(s.pos.x, s.pos.y, s.Height + m.HeightOffset),
                                new Vector3(m.Flipped ? -1 : 1, 1, 1),
                                Quaternion.Euler(0, 90 * m.Rotated, 0)
                                ));
                        }
                    }
                }
                return gizmos;
            }
            private (RandomSet<WFCSlot> dirty, WFCState state) InitWFC(Array2D<int> pathDistances, ulong newSeed)
            {
                WFCState state = new();
                RandomSet<WFCSlot> dirty = new(newSeed);
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
                    {
                        WFCSlot s = new(x, y, ref state);
                        state.InitSlot(x, y, s);
                        dirty.Add(s);
                    }
                }
                float maxEntropy = state.GetSlot(0, 0).TotalEntropy;
                WFCTile centerTile = state.GetTile((WorldUtils.WORLD_SIZE + Vector2Int.one) / 2);
                centerTile.slants.Clear();
                centerTile.slants.Add(WorldUtils.Slant.None);

                for (int x = 0; x < pathDistances.Size.x; x++)
                {
                    for (int y = 0; y < pathDistances.Size.y; y++)
                    {
                        int n = pathDistances[x, y];
                        if (n != int.MaxValue)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2Int p = new Vector2Int(x, y) + WorldUtils.CARDINAL_DIRS[i];
                                Vector2Int pp = (new Vector2Int(2 * x + 1, 2 * y + 1) + WorldUtils.CARDINAL_DIRS[i]) / 2;
                                if (!pathDistances.IsInBounds(p) || pathDistances[p.x, p.y] - n == 1 || pathDistances[p.x, p.y] - n == -1)
                                    state.SetValidPassagesAt(pp.x, pp.y, i % 2 == 1, (true, false));
                                else if (pathDistances[p.x, p.y] != int.MaxValue)
                                    state.SetValidPassagesAt(pp.x, pp.y, i % 2 == 1, (false, true));
                            }
                        }
                    }
                }
                return (dirty, state);
            }

            private void UpdateNext(ref WFCState? state, ref RandomSet<WFCSlot> dirty, ref FixedCapacityStack<WFCState> stateStack)
            {
                WFCSlot s = dirty.PopRandom();
                (WFCSlot n, bool backtrack) = s.UpdateValidModules(state);
                if (n is not null)
                    MarkNeighborsDirty(n.pos, n.UpdateConstraints(state), state, ref dirty);
                if (backtrack)
                {
                    Backtrack(ref state, ref dirty, ref stateStack);
                }
                else if (n is not null)
                {
                    state.OverwriteSlot(n);
                }
            }
            void Backtrack(ref WFCState? state, ref RandomSet<WFCSlot> dirty, ref FixedCapacityStack<WFCState> stateStack)
            {
                Debug.Log("Backtrackin' time");
                dirty.Clear();
                Vector2Int lastCollapsedSlot = state.lastCollapsedSlot;
                (int module, int height) lastCollapsedTo = state.lastCollapsedTo;
                if (stateStack.Count == 0)
                {
                    state = null;
                }
                else
                {
                    state = stateStack.Pop();
                    state.RemoveSlotOption(lastCollapsedSlot, lastCollapsedTo);
                    MarkDirty(lastCollapsedSlot.x, lastCollapsedSlot.y, state, ref dirty);
                }
            }

        }

        public static void MarkNeighborsDirty(Vector2Int pos, IEnumerable<Vector2Int> offsets, in WFCState state, ref RandomSet<WFCSlot> dirty)
        {
            foreach (var offset in offsets)
            {
                MarkDirty(pos.x + offset.x, pos.y + offset.y, state, ref dirty);
            }
        }
        public static void MarkDirty(int x, int y, in WFCState state, ref RandomSet<WFCSlot> dirty)
        {
            if (!WorldUtils.IsInRange(x, y, WorldUtils.WORLD_SIZE.x + 1, WorldUtils.WORLD_SIZE.y + 1))
                return;
            WFCSlot s = state.GetSlot(x, y);
            if (s is null || s.Collapsed != -1)
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
