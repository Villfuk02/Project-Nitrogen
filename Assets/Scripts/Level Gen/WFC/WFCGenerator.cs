using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFCGenerator : LevelGeneratorPart
{
    [Header("References")]
    public GameObject slotPrefab;
    [Header("Init Data")]
    public WFCModule[] moduleSetup;
    public Gradient pathDistanceGradient;
    [Header("Runtime")]
    public static int stepsPerTick = 20;
    public static int steps = 0;
    public static WFCModule[] ALL_MODULES;
    public static WFCState state = null;
    static readonly RandomSet<WFCSlot> dirty = new();

    const int BACKUP_DEPTH = 8;
    static readonly FixedStack<WFCState> stateStack = new(BACKUP_DEPTH);
    public static float maxEntropy = 0;

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
            f.graphicsHeightOffset += f.heightOffsets.x;
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
        int div = (twoOnly ? 2 : 1);
        WFCModule[] n = new WFCModule[m.Length * 4 / div];
        for (int i = 0; i < m.Length; i++)
        {
            m[i].weight *= 0.25f * div;
            Vector3Int[] heightRotations = new Vector3Int[4];
            int[] graphicsHeightRotations = new int[4];
            heightRotations[0] = m[i].heightOffsets;
            graphicsHeightRotations[0] = m[i].graphicsHeightOffset;
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
                c.graphicsHeightOffset = graphicsHeightRotations[r];
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

    public override void Init()
    {
        GenerateModuleVariations();
        StartCoroutine(DoWFC());
    }

    private IEnumerator DoWFC()
    {
        System.DateTime start = System.DateTime.Now;
        InitWFC();
        while (state.uncollapsed > 0)
        {
            while (dirty.Count > 0)
            {
                UpdateNext();
            }
            Backup();
            state.CollapseRandom();
        }
        Debug.Log($"WFC finished in {System.DateTime.Now - start}");
        yield return null;
        stopped = true;
    }

    private void InitWFC()
    {
        state = new WFCState();
        for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
        {
            for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
            {
                WFCSlotDisplay sd = Instantiate(slotPrefab, WorldUtils.SlotToWorldPos(x, y), Quaternion.identity, transform).GetComponent<WFCSlotDisplay>();
                WFCSlot s = new(ALL_MODULES.Length, x, y);
                sd.slotPos = s.pos;
                state.InitSlot(x, y, s);
                MarkDirty(x, y);
            }
        }
        maxEntropy = state.GetSlot(0, 0).TotalEntropy;
        WFCTile centerTile = state.GetTile((WorldUtils.WORLD_SIZE + Vector2Int.one) / 2);
        centerTile.slants.Clear();
        centerTile.slants.Add(WorldUtils.Slant.None);

        for (int x = 0; x < PathGenerator.nodes.GetLength(0); x++)
        {
            for (int y = 0; y < PathGenerator.nodes.GetLength(1); y++)
            {
                int n = PathGenerator.nodes[x, y];
                if (n != int.MaxValue)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2Int p = new Vector2Int(x, y) + WorldUtils.CARDINAL_DIRS[i];
                        Vector2Int pp = (new Vector2Int(2 * x + 1, 2 * y + 1) + WorldUtils.CARDINAL_DIRS[i]) / 2;
                        if (p.x < 0 || p.y < 0 || p.x >= PathGenerator.nodes.GetLength(0) || p.y >= PathGenerator.nodes.GetLength(1) || PathGenerator.nodes[p.x, p.y] - n == 1 || PathGenerator.nodes[p.x, p.y] - n == -1)
                            state.SetValidPassagesAt(pp.x, pp.y, i % 2 == 1, (true, false));
                        else if (PathGenerator.nodes[p.x, p.y] != int.MaxValue)
                            state.SetValidPassagesAt(pp.x, pp.y, i % 2 == 1, (false, true));
                    }
                }
            }
        }
    }

    public static void MarkNeighborsDirty(Vector2Int pos, IEnumerable<Vector2Int> offsets)
    {
        foreach (var offset in offsets)
        {
            MarkDirty(pos.x + offset.x, pos.y + offset.y);
        }
    }
    public static void MarkDirty(int x, int y)
    {
        if (!WorldUtils.IsInRange(x, y, WorldUtils.WORLD_SIZE.x + 1, WorldUtils.WORLD_SIZE.y + 1))
            return;
        WFCSlot s = state.GetSlot(x, y);
        if (s == null || s.Collapsed != -1)
            return;
        dirty.TryAdd(s);
    }
    private void UpdateNext()
    {
        WFCSlot s = dirty.PopRandom();
        (WFCSlot n, bool backtrack) = s.Update();
        if (backtrack)
        {
            Backtrack();
        }
        else if (n != null)
        {
            state.OverwriteSlot(n);
        }
    }
    public static bool IsDirty(WFCSlot s)
    {
        return dirty.Contains(s);
    }
    public void Backup()
    {
        stateStack.Push(new WFCState(state));
    }
    public void Backtrack()
    {
        Debug.Log("Backtrackin' time");
        dirty.Clear();
        Vector2Int lastCollapsedSlot = state.lastCollapsedSlot;
        (int module, int height) lastCollapsedTo = state.lastCollapsedTo;
        if (stateStack.Count == 0)
        {
            throw new System.Exception($"Invalid Settings - not satisfiable");
        }
        else
        {
            state = stateStack.Pop();
            state.RemoveSlotOption(lastCollapsedSlot, lastCollapsedTo);
        }
    }
}
