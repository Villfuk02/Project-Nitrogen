using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGenOld.WFC
{
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
                float w = WFCGenerator.ALL_MODULES[i].weight;
                weights[w] = (weights.ContainsKey(w) ? weights[w] : 0) + WorldUtils.MAX_HEIGHT + 1;
            }
            pos = new Vector2Int(x, y);
            totalEntropy = CalculateEntropy(weights);
            WFCGenerator.state.uncollapsed++;
            WFCGenerator.state.entropyQueue.Add(pos, 0.001f);
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
                    weightAccumulator += WFCGenerator.ALL_MODULES[m].weight;
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
            WFCTile[] vTiles = WFCGenerator.state.GetVaildTilesAtSlot(pos);

            bool changed = false;
            WFCSlot n = new(pos);
            Dictionary<float, int> weights = new();

            for (int i = validModules.Count - 1; i >= 0; i--)
            {
                WFCModule module = WFCGenerator.ALL_MODULES[validModules[i]];
                bool invalid = false;
                for (int d = 0; d < 4; d++)
                {
                    if (!(module.passable[d] ? vPassages[d].passable : vPassages[d].unpassable) || !vTiles[d].terrainTypes.Contains(module.terrainTypes[d]) || !vTiles[d].slants.Contains(module.slants[d]))
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
                            && vTiles[0].heights.Contains(h)
                            && vTiles[1].heights.Contains(h + module.heightOffsets.x)
                            && vTiles[2].heights.Contains(h + module.heightOffsets.y)
                            && vTiles[3].heights.Contains(h + module.heightOffsets.z))
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
            WFCGenerator.state.entropyQueue.UpdateWeight(pos, WFCGenerator.maxEntropy + 0.001f - n.totalEntropy);
            if (n.validModules.Count == 0)
                return (null, true);
            return (n, false);
        }
        HashSet<Vector2Int> UpdateConstraints()
        {
            //Init Available
            (bool passable, bool unpassable)[] aPassages = new (bool, bool)[4];
            WFCTile[] aTiles = new WFCTile[4];
            for (int i = 0; i < 4; i++)
            {
                aTiles[i] = new(false);
            }

            //Calculate Available
            foreach (int m in validModules)
            {
                WFCModule module = WFCGenerator.ALL_MODULES[m];
                for (int i = 0; i < 4; i++)
                {
                    if (module.passable[i])
                        aPassages[i].passable = true;
                    else
                        aPassages[i].unpassable = true;
                    aTiles[i].terrainTypes.Add(module.terrainTypes[i]);
                    aTiles[i].slants.Add(module.slants[i]);
                }
                foreach (int h in validHeights[m])
                {
                    aTiles[0].heights.Add(h);
                    aTiles[1].heights.Add(h + module.heightOffsets.x);
                    aTiles[2].heights.Add(h + module.heightOffsets.y);
                    aTiles[3].heights.Add(h + module.heightOffsets.z);
                }
            }

            //Previous
            (bool passable, bool unpassable)[] pPassages = WFCGenerator.state.GetValidPassagesAtSlot(pos.x, pos.y);
            WFCTile[] pTiles = WFCGenerator.state.GetVaildTilesAtSlot(pos);
            //New
            HashSet<Vector2Int> toUpdate = new();
            for (int i = 0; i < 4; i++)
            {
                if (pPassages[i].passable == aPassages[i].passable && pPassages[i].unpassable == aPassages[i].unpassable)
                {
                    aPassages[i] = pPassages[i];
                }
                else
                {
                    aPassages[i] = (pPassages[i].passable && aPassages[i].passable, pPassages[i].unpassable && aPassages[i].unpassable);
                    toUpdate.Add(WorldUtils.CARDINAL_DIRS[i]);
                }
                if (pTiles[i].heights.IsSubsetOf(aTiles[i].heights) && !pTiles[i].terrainTypes.IsSubsetOf(aTiles[i].terrainTypes) && !pTiles[i].slants.IsSubsetOf(aTiles[i].slants))
                {
                    aTiles[i] = pTiles[i];
                }
                else
                {
                    aTiles[i].heights.IntersectWith(pTiles[i].heights);
                    aTiles[i].terrainTypes.IntersectWith(pTiles[i].terrainTypes);
                    aTiles[i].slants.IntersectWith(pTiles[i].slants);
                    Vector2Int a = WorldUtils.CARDINAL_DIRS[(i + 3) % 4];
                    Vector2Int b = WorldUtils.CARDINAL_DIRS[i];
                    toUpdate.Add(a);
                    toUpdate.Add(a + b);
                    toUpdate.Add(b);
                }
            }
            //Update State
            WFCGenerator.state.SetValidPassagesAtSlot(pos.x, pos.y, aPassages);
            WFCGenerator.state.SetValidTilesAtSlot(pos, aTiles);

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
}
