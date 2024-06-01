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

        readonly List<int> validModules_;
        readonly BitSet32[] validHeights_;

        TilesData.CollapsedSlot invalidModule_ = TilesData.CollapsedSlot.NONE;

        public TilesData.CollapsedSlot Collapsed { get; private set; } = TilesData.CollapsedSlot.NONE;

        public WFCSlot(Vector2Int pos, ref WFCState state, int? forcedHeight = null)
        {
            int moduleCount = WorldGenerator.TerrainType.Modules.Length;
            validModules_ = new(moduleCount);
            validHeights_ = new BitSet32[moduleCount];
            for (int m = 0; m < moduleCount; m++)
            {
                validModules_.Add(m);
                if (forcedHeight is int forced)
                    validHeights_[m] = BitSet32.OneBit(forced);
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
            validHeights_ = new BitSet32[WorldGenerator.TerrainType.Modules.Length];
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
            {
                var module = WorldGenerator.TerrainType.Modules[m];
                foreach (int h in validHeights_[m].GetBits())
                    possibilities.AddOrUpdate(new(module, m, h), module.Weight);
            }

            var slot = possibilities.PopRandom();
            n.Collapsed = slot;
            n.validModules_.Add(slot.moduleIndex);
            n.validHeights_[slot.moduleIndex] = BitSet32.OneBit(slot.height);
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
        /// <returns>newSlot - null if it didn't change or backtracking is needed, otherwise the updated slot. backtrack - true if the generator needs to backtrack.</returns>
        public (WFCSlot newSlot, bool backtrack) UpdateValidModules(in WFCState state)
        {
            var vPassages = state.GetValidEdgesAtSlot(pos);
            var vTiles = state.GetValidTilesAtSlot(pos);

            bool changed = false;
            WFCSlot n = new(pos);

            foreach (var m in validModules_)
            {
                ModuleShape moduleShape = WorldGenerator.TerrainType.Modules[m].Shape;
                if (!AreEdgesConsistent(moduleShape, vPassages, vTiles))
                {
                    changed = true;
                    continue;
                }

                var newHeights = GetConsistentHeights(m, moduleShape, vTiles);
                if (newHeights != validHeights_[m])
                    changed = true;
                if (newHeights.IsEmpty)
                    continue;

                n.validModules_.Add(m);
                n.validHeights_[m] = newHeights;
            }

            if (!changed)
                return (null, false);
            if (n.validModules_.Count == 0)
                return (null, true);

            return (n, false);
        }

        BitSet32 GetConsistentHeights(int moduleIndex, ModuleShape moduleShape, DiagonalDirs<WFCTile> vTiles)
        {
            var newHeights = validHeights_[moduleIndex];
            var moduleCornerHeights = moduleShape.Heights;
            for (int d = 0; d < 4; d++)
            {
                var realCornerHeights = newHeights << moduleCornerHeights[d];
                var alignsWithTile = BitSet32.Intersect(vTiles[d].heights, realCornerHeights);
                newHeights = alignsWithTile >> moduleCornerHeights[d];
            }

            if (invalidModule_.moduleIndex == moduleIndex)
                newHeights.ResetBit(invalidModule_.height);

            return newHeights;
        }

        static bool AreEdgesConsistent(ModuleShape moduleShape, CardinalDirs<BitSet32> vEdges, DiagonalDirs<WFCTile> vTiles)
        {
            for (int d = 0; d < 4; d++)
            {
                if (!vEdges[d].IsSet(moduleShape.Edges[d]) || !vTiles[d].surfaces.IsSet(moduleShape.Surfaces[d]) || !vTiles[d].slants.IsSet((int)moduleShape.Slants[d]))
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
            // values current modules support
            var edges = new CardinalDirs<BitSet32>();
            var tiles = new DiagonalDirs<WFCTile>();
            for (int i = 0; i < 4; i++)
                tiles[i] = new(false);
            GetSupportedConstraintValues(ref edges, ref tiles);

            // previously allowed values
            var prevEdges = state.GetValidEdgesAtSlot(pos);
            var prevTiles = state.GetValidTilesAtSlot(pos);

            // newly allowed values
            var neighborsToUpdate = MergeConstraintValues(prevEdges, prevTiles, ref edges, ref tiles);

            // update state
            state.SetValidEdgesAtSlot(pos, edges);
            state.SetValidTilesAtSlot(pos, tiles);

            return neighborsToUpdate;
        }

        /// <summary>
        /// Computes the intersection of the previously allowed values with the currently supported values.
        /// Returns neighbors at which offsets should be recalculated, due to some values changing.
        /// </summary>
        static HashSet<Vector2Int> MergeConstraintValues(CardinalDirs<BitSet32> prevEdges, DiagonalDirs<WFCTile> prevTiles, ref CardinalDirs<BitSet32> edges, ref DiagonalDirs<WFCTile> tiles)
        {
            var toUpdate = new HashSet<Vector2Int>();
            for (int d = 0; d < 4; d++)
            {
                // cardinal constraints
                var prevEdgeTypes = prevEdges[d];
                if (prevEdgeTypes.IsSubsetOf(edges[d]))
                {
                    // no new restriction
                    edges[d] = prevEdgeTypes;
                }
                else
                {
                    edges[d].IntersectWith(prevEdgeTypes);
                    toUpdate.Add(WorldUtils.CARDINAL_DIRS[d]);
                }

                // diagonal constraints
                var prevTile = prevTiles[d];
                if (prevTile.heights.IsSubsetOf(tiles[d].heights) && prevTile.surfaces.IsSubsetOf(tiles[d].surfaces) && prevTile.slants.IsSubsetOf(tiles[d].slants))
                {
                    // no new restriction
                    tiles[d] = prevTile;
                }
                else
                {
                    tiles[d].heights.IntersectWith(prevTile.heights);
                    tiles[d].surfaces.IntersectWith(prevTile.surfaces);
                    tiles[d].slants.IntersectWith(prevTile.slants);
                    Vector2Int a = WorldUtils.CARDINAL_DIRS[(d + 3) % 4];
                    Vector2Int b = WorldUtils.CARDINAL_DIRS[d];
                    toUpdate.Add(a);
                    toUpdate.Add(a + b);
                    toUpdate.Add(b);
                }
            }

            return toUpdate;
        }

        void GetSupportedConstraintValues(ref CardinalDirs<BitSet32> edges, ref DiagonalDirs<WFCTile> tiles)
        {
            foreach (var m in validModules_)
            {
                ModuleShape moduleShape = WorldGenerator.TerrainType.Modules[m].Shape;
                for (int d = 0; d < 4; d++)
                {
                    var e = edges[d];
                    e.SetBit(moduleShape.Edges[d]);
                    edges[d] = e;

                    tiles[d].surfaces.SetBit(moduleShape.Surfaces[d]);
                    tiles[d].slants.SetBit((int)moduleShape.Slants[d]);
                    tiles[d].heights.UnionWith(validHeights_[m] << moduleShape.Heights[d]);
                }
            }
        }

        public float CalculateEntropy()
        {
            var weights = new Dictionary<float, int>();
            foreach (var m in validModules_)
                weights.Increment(WorldGenerator.TerrainType.Modules[m].Weight, validHeights_[m].PopCount());

            return WFCGenerator.CalculateEntropy(weights);
        }

        public float CalculateCollapseWeight()
        {
            float invalidModules = WorldGenerator.TerrainType.Modules.Length - validModules_.Count;
            return invalidModules + 1;
        }
    }
}