using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFCGenerator : MonoBehaviour
{
    [Header("References")]
    public GameObject slotPrefab;
    [Header("Init Data")]
    public WFCModule[] moduleSetup;
    [Header("Runtime")]
    public static int stepsPerTick = 1;
    public static int steps = 0;
    public static WFCModule[] allModules;
    public static WFCState state = null;
    static readonly RandomizedSet<WFCSlot> dirty = new();

    const int BACKUP_DEPTH = 1;
    static readonly FixedStack<WFCState> stateStack = new(BACKUP_DEPTH);
    public static float maxEntropy = 0;

    public void Awake()
    {
        GenerateModuleVariations();
        StartCoroutine(DoWFC());
    }
    public void Update()
    {
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
        allModules = newModules.ToArray();
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
    public static bool WaitForStep()
    {
        if (stepsPerTick <= 0)
            return false;
        steps++;
        if (steps >= stepsPerTick)
        {
            steps %= stepsPerTick;
            return true;
        }
        return false;
    }

    private IEnumerator DoWFC()
    {
        yield return null;
        yield return null;
        System.DateTime start = System.DateTime.Now;
        InitWFC();
        if (WaitForStep()) yield return null;
        while (state.uncollapsed > 0)
        {
            while (dirty.Count > 0)
            {
                UpdateNext();
                //if (WaitForStep()) yield return null;
            }
            Backup();
            state.CollapseRandom();
            if (WaitForStep()) yield return null;
        }
        Debug.Log($"Generated in {System.DateTime.Now - start}");
    }

    private void InitWFC()
    {
        state = new WFCState();
        for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
        {
            for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
            {
                WFCSlotDisplay sd = Instantiate(slotPrefab, WorldUtils.SlotToWorldPos(x, y), Quaternion.identity, transform).GetComponent<WFCSlotDisplay>();
                WFCSlot s = new(allModules.Length, x, y);
                sd.slotPos = s.pos;
                state.InitSlot(x, y, s);
                MarkDirty(x, y);
            }
        }
        maxEntropy = state.GetSlot(0, 0).TotalEntropy;
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
        dirty.Add(s);
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
        if (stateStack.Count == 0)
        {
            throw new System.Exception($"Invalid Settings - not satisfiable within {BACKUP_DEPTH} backtracks");
        }
        dirty.Clear();
        Vector2Int lastCollapsedSlot = state.lastCollapsedSlot;
        (int module, int height) lastCollapsedTo = state.lastCollapsedTo;
        state = stateStack.Pop();
        state.RemoveSlotOption(lastCollapsedSlot, lastCollapsedTo);
    }
}
