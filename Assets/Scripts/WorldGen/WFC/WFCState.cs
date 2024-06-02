using System.Linq;
using BattleSimulation.World.WorldData;
using UnityEngine;
using Utils;
using Utils.Random;

namespace WorldGen.WFC
{
    /// <summary>
    /// Represents one state of the WFC algorithm. A new state is created from the previous state by collapsing one slot.
    /// </summary>
    public class WFCState
    {
        static readonly DiagonalDirs<Vector2Int> SlotToTileArrayOffsets = new(Vector2Int.up, Vector2Int.one, Vector2Int.right, Vector2Int.zero);
        static readonly CardinalDirs<Vector2Int> TileToSlotArrayOffsets = new(Vector2Int.one, Vector2Int.one, Vector2Int.zero, Vector2Int.zero);
        static readonly CardinalDirs<Vector2Int> SlotToEdgeArrayOffsets = new(Vector2Int.zero, Vector2Int.zero, Vector2Int.down, Vector2Int.left);

        public int uncollapsed;
        public readonly Array2D<WFCSlot> slots;
        readonly Array2D<WFCTile> tiles_;
        // vertical = even slot directions, odd tile directions
        readonly Array2D<BitSet32> verticalEdges_;
        // horizontal = odd slot directions, even tile directions
        readonly Array2D<BitSet32> horizontalEdges_;
        public readonly WeightedRandomSet<Vector2Int> uncollapsedSlots;
        public readonly Array2D<bool> changedSlots;

        public Vector2Int lastCollapsedSlot;
        public TilesData.CollapsedSlot lastCollapsedTo;
        int id_;

        public WFCState()
        {
            Vector2Int slotWorldSize = WorldUtils.WORLD_SIZE + Vector2Int.one;

            slots = new(slotWorldSize);
            tiles_ = new(slotWorldSize + Vector2Int.one);
            foreach (var (index, _) in tiles_.IndexedEnumerable)
                tiles_[index] = new(true);

            var validEdgeTypes = BitSet32.Union(WorldGenerator.TerrainType.FreeEdges, WorldGenerator.TerrainType.BlockedEdges);
            verticalEdges_ = new(slotWorldSize);
            verticalEdges_.Fill(validEdgeTypes);
            horizontalEdges_ = new(slotWorldSize);
            horizontalEdges_.Fill(validEdgeTypes);

            uncollapsedSlots = new(WorldGenerator.Random.NewSeed());
            changedSlots = new(slotWorldSize);
            id_ = 0;
        }

        public WFCState(WFCState original)
        {
            uncollapsed = original.uncollapsed;
            slots = original.slots.Clone();
            tiles_ = original.tiles_.Clone();
            verticalEdges_ = original.verticalEdges_.Clone();
            horizontalEdges_ = original.horizontalEdges_.Clone();
            uncollapsedSlots = new(original.uncollapsedSlots, WorldGenerator.Random.NewSeed());
            changedSlots = new(original.changedSlots.Size);
            id_ = original.id_ + 1000000;
        }

        public void CollapseRandom(WFCGenerator wfc)
        {
            id_++;
            foreach (var (slot, hasChanged) in changedSlots.IndexedEnumerable)
            {
                if (!hasChanged)
                    continue;
                uncollapsedSlots.UpdateWeight(slot, slots[slot].CalculateCollapseWeight());
                changedSlots[slot] = false;
            }

            Vector2Int pos = uncollapsedSlots.PopRandom();
            WFCSlot s = slots[pos];
            lastCollapsedSlot = pos;
            s = s.CollapseRandom(this);
            var updated = s.UpdateConstraints(this);
            wfc.MarkNeighborsDirty(pos, updated);
            lastCollapsedTo = s.Collapsed;
            slots[pos] = s;
        }

        // FINAL GETTERS
        public Array2D<TilesData.CollapsedSlot> GetCollapsedSlots() => new(slots.Select(s => s.Collapsed).ToArray(), slots.Size);

        public bool IsEdgeBlocked(Vector2Int pos, int direction)
        {
            var edges = GetValidEdgesAtSlot(pos + TileToSlotArrayOffsets[direction], 3 - direction);
            edges.IntersectWith(WorldGenerator.TerrainType.FreeEdges);
            return edges.IsEmpty;
        }

        // EDGES
        public CardinalDirs<BitSet32> GetValidEdgesAtSlot(Vector2Int pos)
        {
            var ret = new CardinalDirs<BitSet32>();
            for (int d = 0; d < 4; d++)
                ret[d] = GetValidEdgesAtSlot(pos, d);
            return ret;
        }

        BitSet32 GetValidEdgesAtSlot(Vector2Int pos, int direction)
        {
            if (!slots.IsInBounds(pos + WorldUtils.CARDINAL_DIRS[direction]))
                return BitSet32.AllSet;
            var edgeArray = direction % 2 == 0 ? verticalEdges_ : horizontalEdges_;
            return edgeArray[pos + SlotToEdgeArrayOffsets[direction]];
        }

        public void SetValidEdgesAtSlot(Vector2Int pos, CardinalDirs<BitSet32> validEdges)
        {
            for (int d = 0; d < 4; d++)
                SetValidEdgesAtSlot(pos, d, validEdges[d]);
        }

        void SetValidEdgesAtSlot(Vector2Int pos, int direction, BitSet32 valid)
        {
            if (!slots.IsInBounds(pos + WorldUtils.CARDINAL_DIRS[direction]))
                return;
            var edgeArray = direction % 2 == 0 ? verticalEdges_ : horizontalEdges_;
            edgeArray[pos + SlotToEdgeArrayOffsets[direction]] = valid;
        }

        public void SetValidEdgesAtTile(Vector2Int pos, int direction, bool canBeFree, bool canBeBlocked)
        {
            var slotPos = pos + TileToSlotArrayOffsets[direction];
            var slotDirection = 3 - direction;
            var types = GetValidEdgesAtSlot(slotPos, slotDirection);
            if (!canBeBlocked)
                types.IntersectWith(WorldGenerator.TerrainType.FreeEdges);
            if (!canBeFree)
                types.IntersectWith(WorldGenerator.TerrainType.BlockedEdges);
            SetValidEdgesAtSlot(slotPos, slotDirection, types);
        }

        // TILES
        public DiagonalDirs<WFCTile> GetValidTilesAtSlot(Vector2Int pos)
        {
            return SlotToTileArrayOffsets.Map(o => tiles_[pos + o]);
        }

        public void SetValidTilesAtSlot(Vector2Int pos, DiagonalDirs<WFCTile> newTiles)
        {
            for (int d = 0; d < 4; d++)
            {
                Vector2Int tilePos = pos + SlotToTileArrayOffsets[d];
                tiles_[tilePos] = newTiles[d];
            }
        }

        public WFCTile GetTileAt(Vector2Int pos) => tiles_[pos + Vector2Int.one];


        public override string ToString()
        {
            return $"({id_}) {base.ToString()}";
        }
    }
}