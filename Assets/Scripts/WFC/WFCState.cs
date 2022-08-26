using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFCState
{
    public int uncollapsed = 0;
    readonly WFCSlot[,] slots;
    public readonly RandomizedSet<Vector2Int> entropyQueue = new();
    readonly BitArray passages;
    readonly HashSet<int>[,] heights;
    readonly HashSet<WorldUtils.TerrainType>[,] terrainTypes;
    readonly HashSet<WorldUtils.Slant>[,] slants;

    public Vector2Int lastCollapsedSlot;
    public (int module, int height) lastCollapsedTo;
    public WFCState()
    {
        slots = new WFCSlot[WorldUtils.WORLD_SIZE.x + 1, WorldUtils.WORLD_SIZE.y + 1];
        passages = new BitArray((WorldUtils.WORLD_SIZE.x + 1) * (WorldUtils.WORLD_SIZE.y + 1) * 4, true);
        heights = new HashSet<int>[WorldUtils.WORLD_SIZE.x + 2, WorldUtils.WORLD_SIZE.y + 2];
        terrainTypes = new HashSet<WorldUtils.TerrainType>[WorldUtils.WORLD_SIZE.x + 2, WorldUtils.WORLD_SIZE.y + 2];
        slants = new HashSet<WorldUtils.Slant>[WorldUtils.WORLD_SIZE.x + 2, WorldUtils.WORLD_SIZE.y + 2];
        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                heights[x, y] = new HashSet<int>(WorldUtils.MAX_HEIGHT + 1);
                for (int i = 0; i <= WorldUtils.MAX_HEIGHT; i++)
                {
                    heights[x, y].Add(i);
                }
                terrainTypes[x, y] = new HashSet<WorldUtils.TerrainType>((WorldUtils.TerrainType[])System.Enum.GetValues(typeof(WorldUtils.TerrainType)));
                slants[x, y] = new HashSet<WorldUtils.Slant>((WorldUtils.Slant[])System.Enum.GetValues(typeof(WorldUtils.Slant)));
            }
        }
    }
    public WFCState(WFCState original)
    {
        uncollapsed = original.uncollapsed;
        slots = (WFCSlot[,])original.slots.Clone();
        entropyQueue = new(original.entropyQueue);
        passages = new(original.passages);
        heights = (HashSet<int>[,])original.heights.Clone();
        terrainTypes = (HashSet<WorldUtils.TerrainType>[,])original.terrainTypes.Clone();
        slants = (HashSet<WorldUtils.Slant>[,])original.slants.Clone();
        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                heights[x, y] = new(heights[x, y]);
                terrainTypes[x, y] = new(terrainTypes[x, y]);
                slants[x, y] = new(slants[x, y]);
            }
        }
    }

    public void InitSlot(int x, int y, WFCSlot slot)
    {
        slots[x, y] = slot;
    }

    public WFCSlot GetSlot(int x, int y)
    {
        if (!WorldUtils.IsInRange(x, y, slots.GetLength(0), slots.GetLength(1)))
            return null;
        return slots[x, y];
    }

    public void OverwriteSlot(WFCSlot n)
    {
        slots[n.pos.x, n.pos.y] = n;
    }

    public void CollapseRandom()
    {
        Vector2Int pos = entropyQueue.PopRandom();
        WFCSlot s = slots[pos.x, pos.y];
        lastCollapsedSlot = s.pos;
        s = s.Collapse();
        lastCollapsedTo = (s.Collapsed, s.Height);
        OverwriteSlot(s);
    }
    public void RemoveSlotOption(Vector2Int pos, (int module, int height) module)
    {
        slots[pos.x, pos.y].MarkInvalid(module);
    }
    //PASSAGES
    public (bool passable, bool unpassable)[] GetValidPassagesAtSlot(int x, int y)
    {
        (bool passable, bool unpassable)[] ret = new (bool, bool)[4];
        ret[0] = GetValidPassagesAt(x, y, true);
        ret[1] = GetValidPassagesAt(x, y, false);
        ret[2] = GetValidPassagesAt(x, y - 1, true);
        ret[3] = GetValidPassagesAt(x - 1, y, false);
        return ret;
    }
    private (bool passable, bool unpassable) GetValidPassagesAt(int x, int y, bool vertical)
    {
        if (!WorldUtils.IsInRange(x, y, slots.GetLength(0), slots.GetLength(1)))
            return (true, true);
        int pos = x + y * (WorldUtils.WORLD_SIZE.x + 1) + (vertical ? 0 : (WorldUtils.WORLD_SIZE.x + 1) * (WorldUtils.WORLD_SIZE.y + 1));
        return (passages[pos * 2], passages[pos * 2 + 1]);
    }
    public void SetValidPassagesAtSlot(int x, int y, (bool passable, bool unpassable)[] p)
    {
        SetValidPassagesAt(x, y, true, p[0]);
        SetValidPassagesAt(x, y, false, p[1]);
        SetValidPassagesAt(x, y - 1, true, p[2]);
        SetValidPassagesAt(x - 1, y, false, p[3]);
    }
    private void SetValidPassagesAt(int x, int y, bool vertical, (bool passable, bool unpassable) p)
    {
        if (!WorldUtils.IsInRange(x, y, slots.GetLength(0), slots.GetLength(1)))
            return;
        int pos = x + y * (WorldUtils.WORLD_SIZE.x + 1) + (vertical ? 0 : (WorldUtils.WORLD_SIZE.x + 1) * (WorldUtils.WORLD_SIZE.y + 1));
        passages[pos * 2] = p.passable;
        passages[pos * 2 + 1] = p.unpassable;
    }
    //HEIGHTS
    public HashSet<int>[] GetValidHeightsAtSlot(int x, int y)
    {
        HashSet<int>[] ret = new HashSet<int>[4];
        ret[0] = GetValidHeightsAtTile(x - 1, y);
        ret[1] = GetValidHeightsAtTile(x, y);
        ret[2] = GetValidHeightsAtTile(x, y - 1);
        ret[3] = GetValidHeightsAtTile(x - 1, y - 1);
        return ret;
    }
    private HashSet<int> GetValidHeightsAtTile(int x, int y)
    {
        x++;
        y++;
        if (!WorldUtils.IsInRange(x, y, heights.GetLength(0), heights.GetLength(1)))
            return null;
        return heights[x, y];
    }
    public void SetValidHeightsAtSlot(int x, int y, HashSet<int>[] h)
    {
        SetValidHeightsAtTile(x - 1, y, h[0]);
        SetValidHeightsAtTile(x, y, h[1]);
        SetValidHeightsAtTile(x, y - 1, h[2]);
        SetValidHeightsAtTile(x - 1, y - 1, h[3]);
    }
    private void SetValidHeightsAtTile(int x, int y, HashSet<int> h)
    {
        x++;
        y++;
        if (!WorldUtils.IsInRange(x, y, heights.GetLength(0), heights.GetLength(1)))
            return;
        heights[x, y] = h;
    }
    //TERRAIN TYPES
    public HashSet<WorldUtils.TerrainType>[] GetValidTypesAtSlot(int x, int y)
    {
        HashSet<WorldUtils.TerrainType>[] ret = new HashSet<WorldUtils.TerrainType>[4];
        ret[0] = GetValidTypesAtTile(x - 1, y);
        ret[1] = GetValidTypesAtTile(x, y);
        ret[2] = GetValidTypesAtTile(x, y - 1);
        ret[3] = GetValidTypesAtTile(x - 1, y - 1);
        return ret;
    }
    private HashSet<WorldUtils.TerrainType> GetValidTypesAtTile(int x, int y)
    {
        x++;
        y++;
        if (!WorldUtils.IsInRange(x, y, terrainTypes.GetLength(0), terrainTypes.GetLength(1)))
            return null;
        return terrainTypes[x, y];
    }
    public void SetValidTypesAtSlot(int x, int y, HashSet<WorldUtils.TerrainType>[] h)
    {
        SetValidTypesAtTile(x - 1, y, h[0]);
        SetValidTypesAtTile(x, y, h[1]);
        SetValidTypesAtTile(x, y - 1, h[2]);
        SetValidTypesAtTile(x - 1, y - 1, h[3]);
    }
    private void SetValidTypesAtTile(int x, int y, HashSet<WorldUtils.TerrainType> t)
    {
        x++;
        y++;
        if (!WorldUtils.IsInRange(x, y, terrainTypes.GetLength(0), terrainTypes.GetLength(1)))
            return;
        terrainTypes[x, y] = t;
    }
    //SLANTS
    public HashSet<WorldUtils.Slant>[] GetValidSlantsAtSlot(int x, int y)
    {
        HashSet<WorldUtils.Slant>[] ret = new HashSet<WorldUtils.Slant>[4];
        ret[0] = GetValidSlantsAtTile(x - 1, y);
        ret[1] = GetValidSlantsAtTile(x, y);
        ret[2] = GetValidSlantsAtTile(x, y - 1);
        ret[3] = GetValidSlantsAtTile(x - 1, y - 1);
        return ret;
    }
    private HashSet<WorldUtils.Slant> GetValidSlantsAtTile(int x, int y)
    {
        x++;
        y++;
        if (!WorldUtils.IsInRange(x, y, slants.GetLength(0), slants.GetLength(1)))
            return null;
        return slants[x, y];
    }
    public void SetValidSlantsAtSlot(int x, int y, HashSet<WorldUtils.Slant>[] h)
    {
        SetValidSlantsAtTile(x - 1, y, h[0]);
        SetValidSlantsAtTile(x, y, h[1]);
        SetValidSlantsAtTile(x, y - 1, h[2]);
        SetValidSlantsAtTile(x - 1, y - 1, h[3]);
    }
    private void SetValidSlantsAtTile(int x, int y, HashSet<WorldUtils.Slant> t)
    {
        x++;
        y++;
        if (!WorldUtils.IsInRange(x, y, slants.GetLength(0), slants.GetLength(1)))
            return;
        slants[x, y] = t;
    }

}
