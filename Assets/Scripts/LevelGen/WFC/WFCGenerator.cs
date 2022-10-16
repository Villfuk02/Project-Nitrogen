using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.LevelGenerator;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.WFC
{
    public class WFCGenerator : MonoBehaviour
    {
        [Header("References")]
        [Header("Init Data")]
        [SerializeField] WFCModule[] moduleSetup;
        [Header("Runtime")]
        public static WFCModule[] ALL_MODULES;
        public static float maxEntropy;
        const int BACKUP_DEPTH = 8;

        public void Prepare()
        {
            GenerateModuleVariations();
        }
        private void GenerateModuleVariations()
        {
            List<WFCModule> newModules = new();
            for (int i = 0; i < moduleSetup.Length; i++)
            {
                WFCModule module = moduleSetup[i];
                if (module.enabled)
                {
                    WFCModule[] m = { module };
                    if (module.flip)
                        m = FlipVariations(m);
                    if (module.rotate == 2)
                        m = RotateVariations(m, true);
                    else if (module.rotate == 4)
                        m = RotateVariations(m, false);
                    newModules.AddRange(m);
                }
            }
            ALL_MODULES = newModules.ToArray();
        }
        private WFCModule[] FlipVariations(WFCModule[] m)
        {
            WFCModule[] n = new WFCModule[m.Length * 2];
            for (int i = 0; i < m.Length; i++)
            {
                m[i].weight *= 0.5f;
                WFCModule o = m[i].Copy();
                WFCModule f = m[i].Copy();

                o.flip = false;
                o.name += " O";

                f.name += " F";
                f.passable = new bool[] { f.passable[0], f.passable[3], f.passable[2], f.passable[1] };
                f.meshHeightOffset += f.heightOffsets.x;
                f.heightOffsets = new Vector3Int(-f.heightOffsets.x, f.heightOffsets.z - f.heightOffsets.x, f.heightOffsets.y - f.heightOffsets.x);
                f.terrainTypes = new WorldUtils.TerrainType[] { f.terrainTypes[1], f.terrainTypes[0], f.terrainTypes[3], f.terrainTypes[2] };
                f.slants = new WorldUtils.Slant[] { WorldUtils.FlipSlant(f.slants[1]), WorldUtils.FlipSlant(f.slants[0]), WorldUtils.FlipSlant(f.slants[3]), WorldUtils.FlipSlant(f.slants[2]) };

                n[i * 2] = o;
                n[i * 2 + 1] = f;
            }
            return n;
        }
        private WFCModule[] RotateVariations(WFCModule[] m, bool twoOnly)
        {
            int div = twoOnly ? 2 : 1;
            WFCModule[] n = new WFCModule[m.Length * 4 / div];
            for (int i = 0; i < m.Length; i++)
            {
                m[i].weight *= 0.25f * div;
                Vector3Int[] heightRotations = new Vector3Int[4];
                int[] graphicsHeightRotations = new int[4];
                heightRotations[0] = m[i].heightOffsets;
                graphicsHeightRotations[0] = m[i].meshHeightOffset;
                for (int r = 1; r < 4; r++)
                {
                    Vector3Int h = heightRotations[r - 1];
                    graphicsHeightRotations[r] = graphicsHeightRotations[r - 1] + h.z;
                    heightRotations[r] = new Vector3Int(-h.z, h.x - h.z, h.y - h.z);
                }
                for (int r = 0; r < 4; r += div)
                {
                    WFCModule c = m[i].Copy();
                    c.name += " " + r;
                    c.rotate = r;
                    c.passable = new bool[] { c.passable[(4 - r) % 4], c.passable[(5 - r) % 4], c.passable[(6 - r) % 4], c.passable[(7 - r) % 4] };
                    c.heightOffsets = heightRotations[r];
                    c.meshHeightOffset = graphicsHeightRotations[r];
                    c.terrainTypes = new WorldUtils.TerrainType[] { c.terrainTypes[(4 - r) % 4], c.terrainTypes[(5 - r) % 4], c.terrainTypes[(6 - r) % 4], c.terrainTypes[(7 - r) % 4] };
                    c.slants = new WorldUtils.Slant[] {
                    WorldUtils.RotateSlant(c.slants[(4 - r) % 4], r),
                    WorldUtils.RotateSlant(c.slants[(5 - r) % 4], r),
                    WorldUtils.RotateSlant(c.slants[(6 - r) % 4], r),
                    WorldUtils.RotateSlant(c.slants[(7 - r) % 4], r)
                };

                    n[(i * 4 + r) / div] = c;
                }
            }
            return n;
        }

        public JobDataInterface Generate(int[] nodes, out int[] modules, out int[] heights)
        {
            modules = new int[(WorldUtils.WORLD_SIZE.x + 1) * (WorldUtils.WORLD_SIZE.y + 1)];
            heights = new int[(WorldUtils.WORLD_SIZE.x + 1) * (WorldUtils.WORLD_SIZE.y + 1)];
            JobDataInterface jobData = new(Allocator.Persistent);
            JobHandle handle = new GenerateJob
            {
                flatNodes = jobData.Register(nodes, false),
                flatModules = jobData.Register(modules, true),
                flatHeights = jobData.Register(heights, true),
                failed = jobData.RegisterFailed()

            }.Schedule();
            jobData.RegisterHandle(this, handle);
            return jobData;
        }

        struct GenerateJob : IJob
        {
            public NativeArray<int> flatNodes;
            public NativeArray<int> flatModules;
            public NativeArray<int> flatHeights;
            public NativeArray<bool> failed;
            public void Execute()
            {
                WaitForStep(StepType.Phase);
                Debug.Log("Starting WFC");

                int[,] nodes = new int[WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y];
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y; y++)
                    {
                        nodes[x, y] = flatNodes[x + y * WorldUtils.WORLD_SIZE.x];
                    }
                }
                WaitForStep(StepType.Step);
                (RandomSet<WFCSlot> dirty, WFCState state, float maxEntropy) = InitWFC(nodes);
                WFCGenerator.maxEntropy = maxEntropy;
                FixedStack<WFCState> stateStack = new(BACKUP_DEPTH);
                while (state.uncollapsed > 0)
                {
                    while (dirty.Count > 0)
                    {
                        WaitForStep(StepType.Substep);
                        UpdateNext(ref state, ref dirty, ref stateStack);
                        if (state == null)
                        {
                            Debug.Log("WFC failed");
                            failed[0] = true;
                            return;
                        }
                        RegisterGizmos(StepType.Substep, () => DrawEntropy(state, dirty));
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
                    float entropy = maxEntropy - weight;
                    float size;
                    if (entropy <= 2)
                    {
                        size = entropy * 0.5f;
                        c += Color.green;
                    }
                    else
                    {
                        size = entropy / maxEntropy;
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
                            WFCModule m = ALL_MODULES[s.Collapsed];
                            gizmos.Add(new GizmoManager.Mesh(
                                Color.white,
                                m.mesh,
                                WorldUtils.SlotToWorldPos(s.pos.x, s.pos.y, s.Height - m.meshHeightOffset),
                                new Vector3(m.flip ? -1 : 1, 1, 1),
                                Quaternion.Euler(0, 90 * m.rotate, 0)
                                ));
                        }
                    }
                }
                return gizmos;
            }
            private (RandomSet<WFCSlot> dirty, WFCState state, float maxEntropy) InitWFC(int[,] nodes)
            {
                WFCState state = new();
                RandomSet<WFCSlot> dirty = new();
                for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
                {
                    for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
                    {
                        WFCSlot s = new(ALL_MODULES.Length, x, y, ref state);
                        state.InitSlot(x, y, s);
                        dirty.Add(s);
                    }
                }
                float maxEntropy = state.GetSlot(0, 0).TotalEntropy;
                WFCTile centerTile = state.GetTile((WorldUtils.WORLD_SIZE + Vector2Int.one) / 2);
                centerTile.slants.Clear();
                centerTile.slants.Add(WorldUtils.Slant.None);

                for (int x = 0; x < nodes.GetLength(0); x++)
                {
                    for (int y = 0; y < nodes.GetLength(1); y++)
                    {
                        int n = nodes[x, y];
                        if (n != int.MaxValue)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2Int p = new Vector2Int(x, y) + WorldUtils.CARDINAL_DIRS[i];
                                Vector2Int pp = (new Vector2Int(2 * x + 1, 2 * y + 1) + WorldUtils.CARDINAL_DIRS[i]) / 2;
                                if (p.x < 0 || p.y < 0 || p.x >= nodes.GetLength(0) || p.y >= nodes.GetLength(1) || nodes[p.x, p.y] - n == 1 || nodes[p.x, p.y] - n == -1)
                                    state.SetValidPassagesAt(pp.x, pp.y, i % 2 == 1, (true, false));
                                else if (nodes[p.x, p.y] != int.MaxValue)
                                    state.SetValidPassagesAt(pp.x, pp.y, i % 2 == 1, (false, true));
                            }
                        }
                    }
                }
                return (dirty, state, maxEntropy);
            }

            private void UpdateNext(ref WFCState state, ref RandomSet<WFCSlot> dirty, ref FixedStack<WFCState> stateStack)
            {
                WFCSlot s = dirty.PopRandom();
                (WFCSlot n, bool backtrack) = s.UpdateValidModules(state);
                if (n != null)
                    MarkNeighborsDirty(n.pos, n.UpdateConstraints(state), state, ref dirty);
                if (backtrack)
                {
                    Backtrack(ref state, ref dirty, ref stateStack);
                }
                else if (n != null)
                {
                    state.OverwriteSlot(n);
                }
            }
            void Backtrack(ref WFCState state, ref RandomSet<WFCSlot> dirty, ref FixedStack<WFCState> stateStack)
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
            if (s == null || s.Collapsed != -1)
                return;
            dirty.TryAdd(s);
        }
    }
}
