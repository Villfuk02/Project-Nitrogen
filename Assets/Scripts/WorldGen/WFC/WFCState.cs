using BattleSimulation.World.WorldData;
using System.Collections;
using System.Linq;
using UnityEngine;
using Utils;
using Utils.Random;

namespace WorldGen.WFC
{
    public class WFCState
    {
        static readonly DiagonalDirs<Vector2Int> SlotToTileArrayOffsets = new(Vector2Int.up, Vector2Int.one, Vector2Int.right, Vector2Int.zero);
        static readonly CardinalDirs<Vector2Int> TileToSlotArrayOffsets = new(Vector2Int.one, Vector2Int.one, Vector2Int.zero, Vector2Int.zero);
        static readonly CardinalDirs<int> SlotToPassageArrayOffsets = new(0, 2, -4 * (WorldUtils.WORLD_SIZE.x + 1), -2);

        public int uncollapsed;
        public readonly Array2D<WFCSlot> slots;
        readonly Array2D<WFCTile> tiles_;
        readonly BitArray passages_;
        public readonly WeightedRandomSet<Vector2Int> entropyQueue;

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
            passages_ = new(slotWorldSize.x * slotWorldSize.y * 4, true);
            entropyQueue = new(WorldGenerator.Random.NewSeed());
            id_ = 0;
        }
        public WFCState(WFCState original)
        {
            uncollapsed = original.uncollapsed;
            slots = original.slots.Clone();
            tiles_ = original.tiles_.Clone();
            passages_ = new(original.passages_);
            entropyQueue = new(original.entropyQueue, WorldGenerator.Random.NewSeed());
            id_ = original.id_ + 1000000;
        }

        public void CollapseRandom(WFCGenerator wfc)
        {
            id_++;
            Vector2Int pos = entropyQueue.PopRandom();
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
        public bool GetPassageAtTile(Vector2Int pos, int direction)
        {
            (bool passable, bool _) = GetValidPassageAtSlot(pos + TileToSlotArrayOffsets[direction], 3 - direction);
            return passable;
        }

        // PASSAGES
        public CardinalDirs<(bool passable, bool impassable)> GetValidPassagesAtSlot(Vector2Int pos)
        {
            var ret = new CardinalDirs<(bool passable, bool impassable)>();
            for (int i = 0; i < 4; i++)
            {
                ret[i] = GetValidPassageAtSlot(pos, i);
            }
            return ret;
        }
        (bool passable, bool impassable) GetValidPassageAtSlot(Vector2Int pos, int direction)
        {
            if (!slots.IsInBounds(pos + WorldUtils.CARDINAL_DIRS[direction]))
                return (true, true);
            int index = GetPassageArrayIndex(pos, direction);
            return (passages_[index], passages_[index + 1]);
        }

        public void SetValidPassagesAtSlot(Vector2Int pos, CardinalDirs<(bool passable, bool impassable)> p)
        {
            for (int i = 0; i < 4; i++)
            {
                SetValidPassageAtSlot(pos, i, p[i]);
            }
        }
        void SetValidPassageAtSlot(Vector2Int pos, int direction, (bool passable, bool impassable) p)
        {
            if (!slots.IsInBounds(pos + WorldUtils.CARDINAL_DIRS[direction]))
                return;
            int index = GetPassageArrayIndex(pos, direction);
            passages_[index] = p.passable;
            passages_[index + 1] = p.impassable;
        }

        public void SetValidPassageAtTile(Vector2Int pos, int direction, (bool passable, bool impassable) p)
        {
            SetValidPassageAtSlot(pos + TileToSlotArrayOffsets[direction], 3 - direction, p);
        }

        static int GetPassageArrayIndex(Vector2Int slotPos, int direction)
        {
            return (slotPos.x + slotPos.y * (WorldUtils.WORLD_SIZE.x + 1)) * 4 + SlotToPassageArrayOffsets[direction];
        }
        // TILES
        public DiagonalDirs<WFCTile> GetValidTilesAtSlot(Vector2Int pos)
        {
            return SlotToTileArrayOffsets.Map(o => tiles_[pos + o]);
        }
        public void SetValidTilesAtSlot(Vector2Int pos, DiagonalDirs<WFCTile> newTiles)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2Int tilePos = pos + SlotToTileArrayOffsets[i];
                tiles_[tilePos] = newTiles[i];
            }
        }
        public WFCTile GetTileAt(Vector2Int pos) => tiles_[pos + Vector2Int.one];


        public override string ToString()
        {
            return $"({id_}) {base.ToString()}";
        }
    }
}