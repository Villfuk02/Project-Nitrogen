using System.Collections.Generic;
using BattleSimulation.World.WorldData;
using Data.WorldGen;
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
        readonly Dictionary<Module, BitSet32> validHeights_;

        TilesData.CollapsedSlot invalidModule_ = TilesData.CollapsedSlot.NONE;

        public TilesData.CollapsedSlot Collapsed { get; private set; } = TilesData.CollapsedSlot.NONE;

        public WFCSlot(Vector2Int pos, ref WFCState state, int? limitHeight = null)
        {
            int moduleCount = WorldGenerator.TerrainType.Modules.Length;
            validModules_ = new(moduleCount);
            validHeights_ = new(moduleCount);
            for (int i = 0; i < moduleCount; i++)
            {
                Module m = WorldGenerator.TerrainType.Modules[i];
                validModules_.Add(m);
                if (limitHeight is int limit)
                    validHeights_[m] = BitSet32.OneBit(limit);
                else
                    validHeights_[m] = BitSet32.LowestBitsSet(WorldGenerator.TerrainType.MaxHeight + 1);
            }

            this.pos = pos;
            state.uncollapsed++;
            state.uncollapsedSlots.AddOrUpdate(pos, 1);
        }

        public WFCSlot(Vector2Int pos)
        {
            this.pos = pos;
            validModules_ = new();
            validHeights_ = new();
        }

        /// <summary>
        /// Collapses the slot to a random module.
        /// </summary>
        /// <returns>The new slot after collapsing.</returns>
        public WFCSlot CollapseRandom(WFCState state)
        {
            WFCSlot n = new(pos);

            var possibilities = new WeightedRandomSet<TilesData.CollapsedSlot>(WorldGenerator.Random.NewSeed());

            foreach (var m in validModules_)
            foreach (int h in validHeights_[m].GetBits())
                possibilities.AddOrUpdate(new() { module = m, height = h }, m.Weight);

            var slot = possibilities.PopRandom();
            n.Collapsed = slot;
            n.validModules_.Add(slot.module);
            n.validHeights_[slot.module] = BitSet32.OneBit(slot.height);
            state.uncollapsed--;
            return n;
        }

        /// <summary>
        /// Marks the given module as invalid, to be removed from the possible modules on the next valid module recalculation.
        /// </summary>
        public void MarkInvalid(TilesData.CollapsedSlot invalid)
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

            foreach (var module in validModules_)
            {
                if (!CheckEdges(module, vPassages, vTiles))
                {
                    changed = true;
                    continue;
                }

                var newHeights = CheckHeights(module, vTiles);
                if (newHeights != validHeights_[module])
                    changed = true;
                if (newHeights.IsEmpty)
                    continue;

                n.validModules_.Add(module);
                n.validHeights_[module] = newHeights;
            }

            if (!changed)
                return (null, false);
            if (n.validModules_.Count == 0)
                return (null, true);

            return (n, false);
        }

        BitSet32 CheckHeights(Module module, DiagonalDirs<WFCTile> vTiles)
        {
            var newHeights = validHeights_[module];
            var moduleCornerHeights = module.Shape.Heights;
            for (int d = 0; d < 4; d++)
            {
                var realCornerHeights = newHeights << moduleCornerHeights[d];
                var alignsWithTile = BitSet32.Intersect(vTiles[d].heights, realCornerHeights);
                newHeights = alignsWithTile >> moduleCornerHeights[d];
            }

            if (invalidModule_.module == module)
                newHeights.ResetBit(invalidModule_.height);

            return newHeights;
        }

        static bool CheckEdges(Module module, CardinalDirs<(bool passable, bool impassable)> vPassages, DiagonalDirs<WFCTile> vTiles)
        {
            var moduleShape = module.Shape;
            for (int d = 0; d < 4; d++)
            {
                if (
                    (moduleShape.Passable[d] ? !vPassages[d].passable : !vPassages[d].impassable) ||
                    !vTiles[d].surfaces.IsSet(moduleShape.Surfaces[d]) ||
                    !vTiles[d].slants.IsSet((int)moduleShape.Slants[d])
                )
                    return false;
            }

            return true;
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
                if (prevTiles[i].heights == (tiles[i].heights) && prevTiles[i].surfaces != tiles[i].surfaces && prevTiles[i].slants != tiles[i].slants)
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
                for (int d = 0; d < 4; d++)
                {
                    aPassages[d] = (aPassages[d].passable || m.Shape.Passable[d], aPassages[d].impassable || !m.Shape.Passable[d]);
                    aTiles[d].surfaces.SetBit(m.Shape.Surfaces[d]);
                    aTiles[d].slants.SetBit((int)m.Shape.Slants[d]);
                    aTiles[d].heights.UnionWith(validHeights_[m] << m.Shape.Heights[d]);
                }
            }
        }

        public float CalculateEntropy()
        {
            var weights = new Dictionary<float, int>();
            foreach (var module in validModules_)
                weights.Increment(module.Weight, validHeights_[module].PopCount());

            return WFCGenerator.CalculateEntropy(weights);
        }

        public float CalculateWeight()
        {
            float invalidModules = WorldGenerator.TerrainType.Modules.Length - validModules_.Count;
            return invalidModules + 1;
        }
    }
}