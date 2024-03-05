using Data.WorldGen;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Utils.Random;

namespace WorldGen.WFC
{
    /// <summary>
    /// Holds information about what modules at what heights are allowed at this spot. Each slot is in between four tiles.
    /// </summary>
    public class WFCSlot
    {
        public readonly Vector2Int pos;

        readonly List<Module> validModules_;
        readonly Dictionary<Module, HashSet<int>> validHeights_;

        (Module module, int height) invalidModule_ = (null, -1);

        public (Module module, int height) Collapsed { get; private set; } = (null, -1);
        public float TotalEntropy { get; private set; }

        public WFCSlot(Vector2Int pos, ref WFCState state)
        {
            var weights = new Dictionary<float, int>();
            int moduleCount = WorldGenerator.TerrainType.Modules.Length;
            validModules_ = new(moduleCount);
            validHeights_ = new(moduleCount);
            for (int i = 0; i < moduleCount; i++)
            {
                Module m = WorldGenerator.TerrainType.Modules[i];
                validModules_.Add(m);
                validHeights_[m] = Enumerable.Range(0, WorldGenerator.TerrainType.MaxHeight + 1).ToHashSet();
                float w = m.Weight;
                weights.Increment(w, WorldGenerator.TerrainType.MaxHeight + 1);
            }
            this.pos = pos;
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

        public WFCSlot CollapseRandom(WFCState state)
        {
            WFCSlot n = new(pos);

            var possibilities = new WeightedRandomSet<(Module module, int height)>(WorldGenerator.Random.NewSeed());

            foreach (var m in validModules_)
                foreach (int h in validHeights_[m])
                    possibilities.Add((m, h), m.Weight);

            (var module, int height) = possibilities.PopRandom();
            n.Collapsed = (module, height);
            n.validModules_.Add(module);
            n.validHeights_[module] = new() { height };
            state.uncollapsed--;
            return n;
        }

        public void MarkInvalid((Module module, int height) invalid)
        {
            invalidModule_ = invalid;
        }
        /// <summary>
        /// Recalculates which modules and heights are allowed at this slot, based on their boundaries and the adjacent tiles and slots - makes this slot's domain consistent with its neighbors.
        /// When there are no valid modules left, signal the generator to backtrack.
        /// </summary>
        /// <returns>newSlot - null if it didn't change or backtracking is needed, otherwise the updated slot. backtrack - true, if the generator needs to backtrack.</returns>
        public (WFCSlot newSlot, bool backtrack) UpdateValidModules(in WFCState state)
        {
            var vPassages = state.GetValidPassagesAtSlot(pos);
            var vTiles = state.GetValidTilesAtSlot(pos);

            bool changed = false;
            WFCSlot n = new(pos);
            var weights = new Dictionary<float, int>();

            foreach (var module in validModules_)
            {
                if (!CheckEdges(module, vPassages, vTiles))
                {
                    changed = true;
                    continue;
                }

                var newHeights = CheckHeights(module, vTiles);
                if (newHeights.Count < validHeights_[module].Count)
                    changed = true;
                if (newHeights.Count == 0)
                    continue;

                weights.Increment(module.Weight, newHeights.Count);
                n.validModules_.Add(module);
                n.validHeights_[module] = newHeights;
            }
            if (!changed)
                return (null, false);
            if (n.validModules_.Count == 0)
                return (null, true);

            n.TotalEntropy = WFCGenerator.CalculateEntropy(weights);
            state.entropyQueue.UpdateWeight(pos, WFCGenerator.MaxEntropy - n.TotalEntropy + 0.001f);
            return (n, false);
        }

        HashSet<int> CheckHeights(Module module, DiagonalDirs<WFCTile> vTiles)
        {
            var heights = validHeights_[module];
            var newHeights = new HashSet<int>();
            foreach (int h in heights)
            {
                bool neighborsMatch = Enumerable.Range(0, 4).All(d => vTiles[d].heights.Contains(h + module.Shape.Heights[d]));

                if (neighborsMatch && (invalidModule_.module != module || invalidModule_.height != h))
                    newHeights.Add(h);
            }

            return newHeights;
        }

        static bool CheckEdges(Module module, CardinalDirs<(bool passable, bool impassable)> vPassages, DiagonalDirs<WFCTile> vTiles)
        {
            return Enumerable.Range(0, 4).All(d =>
                (module.Shape.Passable[d] ? vPassages[d].passable : vPassages[d].impassable)
                && vTiles[d].surfaces.Contains(module.Shape.Surfaces[d])
                && vTiles[d].slants.Contains(module.Shape.Slants[d])
                );
        }

        /// <summary>
        /// Recalculates this slot's boundary conditions based on the modules and heights allowed here.
        /// </summary>
        /// <returns>Slots at which offsets should be updated (<see cref="UpdateValidModules"/>).</returns>
        public HashSet<Vector2Int> UpdateConstraints(WFCState state)
        {
            // current
            var passages = new CardinalDirs<(bool passable, bool impassable)>();
            var tiles = new DiagonalDirs<WFCTile>();
            for (int i = 0; i < 4; i++)
                tiles[i] = new(false);
            CalculateAvailableConstraints(ref passages, ref tiles);

            // previous
            var prevPassages = state.GetValidPassagesAtSlot(pos);
            var prevTiles = state.GetValidTilesAtSlot(pos);

            // final
            var toUpdate = MergeConstraints(prevPassages, prevTiles, ref passages, ref tiles);

            // update state
            state.SetValidPassagesAtSlot(pos, passages);
            state.SetValidTilesAtSlot(pos, tiles);

            return toUpdate;
        }

        static HashSet<Vector2Int> MergeConstraints(CardinalDirs<(bool passable, bool impassable)> prevPassages, DiagonalDirs<WFCTile> prevTiles, ref CardinalDirs<(bool passable, bool impassable)> passages, ref DiagonalDirs<WFCTile> tiles)
        {
            var toUpdate = new HashSet<Vector2Int>();
            for (int i = 0; i < 4; i++)
            {
                // cardinal constraints
                if (prevPassages[i].passable != passages[i].passable || prevPassages[i].impassable != passages[i].impassable)
                {
                    passages[i] = (prevPassages[i].passable && passages[i].passable, prevPassages[i].impassable && passages[i].impassable);
                    toUpdate.Add(WorldUtils.CARDINAL_DIRS[i]);
                }

                // diagonal constraints
                if (prevTiles[i].heights.IsSubsetOf(tiles[i].heights) && !prevTiles[i].surfaces.IsSubsetOf(tiles[i].surfaces) && !prevTiles[i].slants.IsSubsetOf(tiles[i].slants))
                {
                    tiles[i] = prevTiles[i];
                }
                else
                {
                    tiles[i].heights.IntersectWith(prevTiles[i].heights);
                    tiles[i].surfaces.IntersectWith(prevTiles[i].surfaces);
                    tiles[i].slants.IntersectWith(prevTiles[i].slants);
                    Vector2Int a = WorldUtils.CARDINAL_DIRS[(i + 3) % 4];
                    Vector2Int b = WorldUtils.CARDINAL_DIRS[i];
                    toUpdate.Add(a);
                    toUpdate.Add(a + b);
                    toUpdate.Add(b);
                }
            }

            return toUpdate;
        }

        void CalculateAvailableConstraints(ref CardinalDirs<(bool passable, bool impassable)> aPassages, ref DiagonalDirs<WFCTile> aTiles)
        {
            foreach (var m in validModules_)
            {
                for (int i = 0; i < 4; i++)
                {
                    aPassages[i] = (aPassages[i].passable || m.Shape.Passable[i], aPassages[i].impassable || !m.Shape.Passable[i]);
                    aTiles[i].surfaces.Add(m.Shape.Surfaces[i]);
                    aTiles[i].slants.Add(m.Shape.Slants[i]);
                    foreach (int h in validHeights_[m])
                    {
                        aTiles[i].heights.Add(h + m.Shape.Heights[i]);
                    }
                }
            }
        }
    }
}
