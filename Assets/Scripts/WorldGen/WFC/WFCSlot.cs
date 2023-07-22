using Data.LevelGen;
using Random;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace WorldGen.WFC
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

        public WFCSlot(int x, int y, ref WFCState state)
        {
            Dictionary<float, int> weights = new();
            int moduleCount = WFCGenerator.Terrain.Modules.Length;
            validModules = new(moduleCount);
            validHeights = new(moduleCount);
            for (int i = 0; i < moduleCount; i++)
            {
                validModules.Add(i);
                validHeights[i] = Enumerable.Range(0, WFCGenerator.Terrain.MaxHeight + 1).ToHashSet();
                float w = WFCGenerator.Terrain.Modules[i].Weight;
                weights[w] = (weights.ContainsKey(w) ? weights[w] : 0) + WFCGenerator.Terrain.MaxHeight + 1;
            }
            pos = new Vector2Int(x, y);
            totalEntropy = WFCGenerator.CalculateEntropy(weights);
            state.uncollapsed++;
            state.entropyQueue.Add(pos, 0.001f);
        }
        public WFCSlot(Vector2Int pos)
        {
            this.pos = pos;
            validModules = new();
            validHeights = new();
        }

        public WFCSlot Collapse(WFCState state)
        {
            WFCSlot n = new(pos);

            WeightedRandomSet<(int height, int module)> possibilities = new(WFCGenerator.Random.NewSeed());

            foreach (int m in validModules)
            {
                float weight = WFCGenerator.Terrain.Modules[m].Weight;
                foreach (int h in validHeights[m])
                {
                    possibilities.Add((h, m), weight);
                }
            }

            (int height, int module) = possibilities.PopRandom();
            n.collapsed = module;
            n.height = height;
            n.validModules.Add(module);
            n.validHeights[module] = new() { height };
            state.uncollapsed--;
            return n;
        }

        public void MarkInvalid((int module, int height) module)
        {
            invalidModule = module;
        }
        public (WFCSlot newSlot, bool backtrack) UpdateValidModules(in WFCState state)
        {
            (bool passable, bool unpassable)[] vPassages = state.GetValidPassagesAtSlot(pos.x, pos.y);
            WFCTile[] vTiles = state.GetVaildTilesAtSlot(pos);

            bool changed = false;
            WFCSlot n = new(pos);
            Dictionary<float, int> weights = new();

            for (int i = validModules.Count - 1; i >= 0; i--)
            {
                Module module = WFCGenerator.Terrain.Modules[validModules[i]];
                bool invalid = false;
                for (int d = 0; d < 4; d++)
                {
                    if (!(module.Shape.Passable[d] ? vPassages[d].passable : vPassages[d].unpassable) || !vTiles[d].surfaces.Contains(module.Shape.Surfaces[d]) || !vTiles[d].slants.Contains(module.Shape.Slants[d]))
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
                            && vTiles[0].heights.Contains(h + module.Shape.Heights[0])
                            && vTiles[1].heights.Contains(h + module.Shape.Heights[1])
                            && vTiles[2].heights.Contains(h + module.Shape.Heights[2])
                            && vTiles[3].heights.Contains(h + module.Shape.Heights[3]))
                        {
                            newHeights.Add(h);
                            weights[module.Weight] = (weights.TryGetValue(module.Weight, out int weight) ? weight : 0) + 1;
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
            n.totalEntropy = WFCGenerator.CalculateEntropy(weights);
            state.entropyQueue.UpdateWeight(pos, WFCGenerator.MaxEntropy + 0.001f - n.totalEntropy);
            if (n.validModules.Count == 0)
                return (null, true);
            return (n, false);
        }
        public HashSet<Vector2Int> UpdateConstraints(WFCState state)
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
                Module module = WFCGenerator.Terrain.Modules[m];
                for (int i = 0; i < 4; i++)
                {
                    if (module.Shape.Passable[i])
                        aPassages[i].passable = true;
                    else
                        aPassages[i].unpassable = true;
                    aTiles[i].surfaces.Add(module.Shape.Surfaces[i]);
                    aTiles[i].slants.Add(module.Shape.Slants[i]);
                }
                foreach (int h in validHeights[m])
                {
                    aTiles[0].heights.Add(h + module.Shape.Heights[0]);
                    aTiles[1].heights.Add(h + module.Shape.Heights[1]);
                    aTiles[2].heights.Add(h + module.Shape.Heights[2]);
                    aTiles[3].heights.Add(h + module.Shape.Heights[3]);
                }
            }

            //Previous
            (bool passable, bool unpassable)[] pPassages = state.GetValidPassagesAtSlot(pos.x, pos.y);
            WFCTile[] pTiles = state.GetVaildTilesAtSlot(pos);
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
                if (pTiles[i].heights.IsSubsetOf(aTiles[i].heights) && !pTiles[i].surfaces.IsSubsetOf(aTiles[i].surfaces) && !pTiles[i].slants.IsSubsetOf(aTiles[i].slants))
                {
                    aTiles[i] = pTiles[i];
                }
                else
                {
                    aTiles[i].heights.IntersectWith(pTiles[i].heights);
                    aTiles[i].surfaces.IntersectWith(pTiles[i].surfaces);
                    aTiles[i].slants.IntersectWith(pTiles[i].slants);
                    Vector2Int a = WorldUtils.CARDINAL_DIRS[(i + 3) % 4];
                    Vector2Int b = WorldUtils.CARDINAL_DIRS[i];
                    toUpdate.Add(a);
                    toUpdate.Add(a + b);
                    toUpdate.Add(b);
                }
            }
            //Update State
            state.SetValidPassagesAtSlot(pos.x, pos.y, aPassages);
            state.SetValidTilesAtSlot(pos, aTiles);

            return toUpdate;
        }
    }
}
