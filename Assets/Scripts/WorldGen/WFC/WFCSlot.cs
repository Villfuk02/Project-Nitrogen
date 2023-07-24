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

        readonly List<Module> validModules_;
        readonly Dictionary<Module, HashSet<int>> validHeights_;

        (Module module, int height) invalidModule_ = (null, -1);

        public Module Collapsed { get; private set; }
        public int Height { get; private set; } = -1;
        public float TotalEntropy { get; private set; }

        public WFCSlot(int x, int y, ref WFCState state)
        {
            Dictionary<float, int> weights = new();
            int moduleCount = WorldGenerator.TerrainType.Modules.Length;
            validModules_ = new(moduleCount);
            validHeights_ = new(moduleCount);
            for (int i = 0; i < moduleCount; i++)
            {
                Module m = WorldGenerator.TerrainType.Modules[i];
                validModules_.Add(m);
                validHeights_[m] = Enumerable.Range(0, WorldGenerator.TerrainType.MaxHeight + 1).ToHashSet();
                float w = m.Weight;
                weights[w] = (weights.TryGetValue(w, out int weight) ? weight : 0) + WorldGenerator.TerrainType.MaxHeight + 1;
            }
            pos = new(x, y);
            TotalEntropy = WFCGenerator.CalculateEntropy(weights);
            state.uncollapsed++;
            state.entropyQueue.Add(pos, 0.001f);
        }
        public WFCSlot(Vector2Int pos)
        {
            this.pos = pos;
            validModules_ = new();
            validHeights_ = new();
        }

        public WFCSlot Collapse(WFCState state)
        {
            WFCSlot n = new(pos);

            WeightedRandomSet<(Module module, int height)> possibilities = new(WorldGenerator.Random.NewSeed());

            foreach (var m in validModules_)
            {
                float weight = m.Weight;
                foreach (int h in validHeights_[m])
                {
                    possibilities.Add((m, h), weight);
                }
            }

            (var module, int height) = possibilities.PopRandom();
            n.Collapsed = module;
            n.Height = height;
            n.validModules_.Add(module);
            n.validHeights_[module] = new() { height };
            state.uncollapsed--;
            return n;
        }

        public void MarkInvalid((Module module, int height) invalid)
        {
            invalidModule_ = invalid;
        }
        public (WFCSlot newSlot, bool backtrack) UpdateValidModules(in WFCState state)
        {
            var vPassages = state.GetValidPassagesAtSlot(pos);
            var vTiles = state.GetValidTilesAtSlot(pos);

            bool changed = false;
            WFCSlot n = new(pos);
            Dictionary<float, int> weights = new();

            for (int i = validModules_.Count - 1; i >= 0; i--)
            {
                Module module = validModules_[i];
                bool invalid = false;
                for (int d = 0; d < 4; d++)
                {
                    if ((module.Shape.Passable[d] ? vPassages[d].passable : vPassages[d].unpassable) &&
                        vTiles[d].surfaces.Contains(module.Shape.Surfaces[d]) &&
                        vTiles[d].slants.Contains(module.Shape.Slants[d]))
                        continue;

                    invalid = true;
                    changed = true;
                    break;
                }
                if (invalid)
                    continue;

                var heights = validHeights_[validModules_[i]];
                HashSet<int> newHeights = new();
                foreach (int h in heights)
                {
                    bool heightsValid = true;
                    for (int d = 0; d < 4; d++)
                    {
                        if (vTiles[d].heights.Contains(h + module.Shape.Heights[d]))
                            continue;
                        heightsValid = false;
                        break;
                    }
                    if (!(invalidModule_.module == validModules_[i] && invalidModule_.height == h) && heightsValid)
                    {
                        newHeights.Add(h);
                        weights[module.Weight] = (weights.TryGetValue(module.Weight, out int weight) ? weight : 0) + 1;
                    }
                    else
                    {
                        changed = true;
                    }
                }
                if (newHeights.Count == 0)
                    continue;

                n.validModules_.Add(validModules_[i]);
                n.validHeights_[validModules_[i]] = newHeights;
            }
            if (!changed)
                return (null, false);
            n.TotalEntropy = WFCGenerator.CalculateEntropy(weights);
            state.entropyQueue.UpdateWeight(pos, WFCGenerator.MaxEntropy + 0.001f - n.TotalEntropy);
            return n.validModules_.Count == 0 ? (null, true) : (n, false);
        }
        public HashSet<Vector2Int> UpdateConstraints(WFCState state)
        {
            //Init Available
            CardinalDirs<(bool passable, bool unpassable)> aPassages = new();
            DiagonalDirs<WFCTile> aTiles = new();
            for (int i = 0; i < 4; i++)
            {
                aTiles[i] = new(false);
            }

            //Calculate Available
            foreach (var m in validModules_)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (m.Shape.Passable[i])
                        aPassages[i] = (true, aPassages[i].unpassable);
                    else
                        aPassages[i] = (aPassages[i].passable, true);
                    aTiles[i].surfaces.Add(m.Shape.Surfaces[i]);
                    aTiles[i].slants.Add(m.Shape.Slants[i]);
                }
                foreach (int h in validHeights_[m])
                {
                    for (int i = 0; i < 4; i++)
                    {
                        aTiles[i].heights.Add(h + m.Shape.Heights[i]);
                    }
                }
            }

            //Previous
            var pPassages = state.GetValidPassagesAtSlot(pos);
            var pTiles = state.GetValidTilesAtSlot(pos);
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
            state.SetValidPassagesAtSlot(pos, aPassages);
            state.SetValidTilesAtSlot(pos, aTiles);

            return toUpdate;
        }
    }
}
