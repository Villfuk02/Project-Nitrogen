using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WFCSlot
{
    public readonly Vector2Int pos;
    int collapsed = -1;
    int height = -1;

    float totalEntropy = 0;
    readonly List<int> validModules;
    readonly Dictionary<int, HashSet<int>> validHeights;

    (int module, int height) invalidModule = (-1, -1);

    public int Collapsed { get => collapsed; }
    public int Height { get => height; }
    public float TotalEntropy { get => totalEntropy; }

    public WFCSlot(int moduleCount, int x, int y)
    {
        Dictionary<float, int> weights = new();
        validModules = new(moduleCount);
        validHeights = new(moduleCount);
        for (int i = 0; i < moduleCount; i++)
        {
            validModules.Add(i);
            validHeights[i] = new HashSet<int>(WorldUtils.MAX_HEIGHT + 1);
            for (int h = 0; h <= WorldUtils.MAX_HEIGHT; h++)
            {
                validHeights[i].Add(h);
            }
            float w = WFCGenerator.allModules[i].weight;
            weights[w] = (weights.ContainsKey(w) ? weights[w] : 0) + (WorldUtils.MAX_HEIGHT + 1);
        }
        pos = new Vector2Int(x, y);
        totalEntropy = CalculateEntropy(weights);
        WFCGenerator.state.uncollapsed++;
        WFCGenerator.state.entropyQueue.Add(pos);
    }
    public WFCSlot(Vector2Int pos)
    {
        this.pos = pos;
        validModules = new();
        validHeights = new();
    }

    public WFCSlot Collapse()
    {
        WFCSlot n = new(pos);

        List<(float stopWeight, int height, int module)> stateScale = new();
        float weightAccumulator = 0;
        foreach (int m in validModules)
        {
            foreach (int h in validHeights[m])
            {
                weightAccumulator += WFCGenerator.allModules[m].weight;
                stateScale.Add((weightAccumulator, h, m));
            }
        }
        float r = Random.Range(0, weightAccumulator);
        (float stopWeight, int height, int module) = stateScale.Find((m) => m.stopWeight >= r);
        n.collapsed = module;
        n.height = height;
        n.validModules.Add(module);
        n.validHeights[module] = new() { height };
        WFCGenerator.state.uncollapsed--;
        WFCGenerator.MarkNeighborsDirty(pos, n.UpdateConstraints());
        return n;
    }

    public void MarkInvalid((int module, int height) module)
    {
        invalidModule = module;
        WFCGenerator.MarkDirty(pos.x, pos.y);
    }

    public (WFCSlot newSlot, bool backtrack) Update()
    {
        (WFCSlot newSlot, bool backtrack) n = UpdateValidModules();
        if (n.newSlot != null)
        {
            WFCGenerator.MarkNeighborsDirty(pos, n.newSlot.UpdateConstraints());
        }
        return n;
    }

    (WFCSlot newSlot, bool backtrack) UpdateValidModules()
    {
        (bool passable, bool unpassable)[] vPassages = WFCGenerator.state.GetValidPassagesAtSlot(pos.x, pos.y);
        HashSet<int>[] vHeights = WFCGenerator.state.GetValidHeightsAtSlot(pos.x, pos.y);
        HashSet<WorldUtils.TerrainType>[] vTypes = WFCGenerator.state.GetValidTypesAtSlot(pos.x, pos.y);
        HashSet<WorldUtils.Slant>[] vSlants = WFCGenerator.state.GetValidSlantsAtSlot(pos.x, pos.y);

        bool changed = false;
        WFCSlot n = new(pos);
        Dictionary<float, int> weights = new();

        for (int i = validModules.Count - 1; i >= 0; i--)
        {
            WFCModule module = WFCGenerator.allModules[validModules[i]];
            bool invalid = false;
            for (int d = 0; d < 4; d++)
            {
                if (!(module.passable[d] ? vPassages[d].passable : vPassages[d].unpassable) || !vTypes[d].Contains(module.terrainTypes[d]) || !vSlants[d].Contains(module.slants[d]))
                {
                    invalid = true;
                    changed = true;
                    break;
                }
            }
            if (!invalid)
            {
                HashSet<int> heights = validHeights[validModules[i]];
                HashSet<int> newHeights = new();
                foreach (int h in heights)
                {
                    if (!(invalidModule.module == validModules[i] && invalidModule.height == h)
                        && vHeights[0].Contains(h)
                        && vHeights[1].Contains(h + module.heightOffsets.x)
                        && vHeights[2].Contains(h + module.heightOffsets.y)
                        && vHeights[3].Contains(h + module.heightOffsets.z))
                    {
                        newHeights.Add(h);
                        weights[module.weight] = (weights.ContainsKey(module.weight) ? weights[module.weight] : 0) + 1;
                    }
                    else
                    {
                        changed = true;
                    }
                }
                if (newHeights.Count > 0)
                {
                    n.validModules.Add(validModules[i]);
                    n.validHeights[validModules[i]] = newHeights;
                }
            }
        }
        if (!changed)
            return (null, false);
        n.totalEntropy = CalculateEntropy(weights);
        if (n.validModules.Count == 0)
            return (null, true);
        return (n, false);
    }
    HashSet<Vector2Int> UpdateConstraints()
    {
        //Init Available
        (bool passable, bool unpassable)[] aPassages = new (bool, bool)[4];
        HashSet<int>[] aHeights = new HashSet<int>[4];
        HashSet<WorldUtils.TerrainType>[] aTypes = new HashSet<WorldUtils.TerrainType>[4];
        HashSet<WorldUtils.Slant>[] aSlants = new HashSet<WorldUtils.Slant>[4];
        for (int i = 0; i < 4; i++)
        {
            aHeights[i] = new();
            aTypes[i] = new();
            aSlants[i] = new();
        }

        //Calculate Available
        foreach (int m in validModules)
        {
            WFCModule module = WFCGenerator.allModules[m];
            for (int i = 0; i < 4; i++)
            {
                if (module.passable[i])
                    aPassages[i].passable = true;
                else
                    aPassages[i].unpassable = true;
                aTypes[i].Add(module.terrainTypes[i]);
                aSlants[i].Add(module.slants[i]);
            }
            foreach (int h in validHeights[m])
            {
                aHeights[0].Add(h);
                aHeights[1].Add(h + module.heightOffsets.x);
                aHeights[2].Add(h + module.heightOffsets.y);
                aHeights[3].Add(h + module.heightOffsets.z);
            }
        }

        //Previous
        (bool passable, bool unpassable)[] pPassages = WFCGenerator.state.GetValidPassagesAtSlot(pos.x, pos.y);
        HashSet<int>[] pHeights = WFCGenerator.state.GetValidHeightsAtSlot(pos.x, pos.y);
        HashSet<WorldUtils.TerrainType>[] pTypes = WFCGenerator.state.GetValidTypesAtSlot(pos.x, pos.y);
        HashSet<WorldUtils.Slant>[] pSlants = WFCGenerator.state.GetValidSlantsAtSlot(pos.x, pos.y);
        //New
        (bool passable, bool unpassable)[] nPassages = new (bool, bool)[4];
        HashSet<int>[] nHeights = new HashSet<int>[4];
        HashSet<WorldUtils.TerrainType>[] nTypes = new HashSet<WorldUtils.TerrainType>[4];
        HashSet<WorldUtils.Slant>[] nSlants = new HashSet<WorldUtils.Slant>[4];
        for (int i = 0; i < 4; i++)
        {
            nPassages[i] = (pPassages[i].passable && aPassages[i].passable, pPassages[i].unpassable && aPassages[i].unpassable);
            aHeights[i].IntersectWith(pHeights[i]);
            nHeights[i] = aHeights[i];
            aTypes[i].IntersectWith(pTypes[i]);
            nTypes[i] = aTypes[i];
            aSlants[i].IntersectWith(pSlants[i]);
            nSlants[i] = aSlants[i];
        }
        //Update State
        WFCGenerator.state.SetValidPassagesAtSlot(pos.x, pos.y, nPassages);
        WFCGenerator.state.SetValidHeightsAtSlot(pos.x, pos.y, nHeights);
        WFCGenerator.state.SetValidTypesAtSlot(pos.x, pos.y, nTypes);
        WFCGenerator.state.SetValidSlantsAtSlot(pos.x, pos.y, nSlants);
        //MarkDirty
        HashSet<Vector2Int> toUpdate = new();
        for (int d = 0; d < 4; d++)
        {
            if (pPassages[d].passable != nPassages[d].passable || pPassages[d].unpassable != nPassages[d].unpassable)
                toUpdate.Add(WorldUtils.CARDINAL_DIRS[d]);
            if (!pHeights[d].IsSubsetOf(nHeights[d]) || !pTypes[d].IsSubsetOf(nTypes[d]) || !pSlants[d].IsSubsetOf(nSlants[d]))
            {
                Vector2Int a = WorldUtils.CARDINAL_DIRS[(d + 3) % 4];
                Vector2Int b = WorldUtils.CARDINAL_DIRS[d];
                toUpdate.Add(a);
                toUpdate.Add(a + b);
                toUpdate.Add(b);
            }
        }
        return toUpdate;
    }

    static float CalculateEntropy(Dictionary<float, int> weights)
    {
        float totalWeight = 0;
        foreach (var w in weights)
        {
            totalWeight += w.Value * w.Key;
        }
        float totalEntropy = 0;
        foreach (var w in weights)
        {
            float p = w.Key / totalWeight;
            totalEntropy -= p * w.Value * Mathf.Log(p, 2);
        }
        return totalEntropy;
    }

}
