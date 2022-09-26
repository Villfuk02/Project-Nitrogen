using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.WFC
{
    public class WFCState
    {
        static readonly Vector2Int[] slotToTileArrayOffsets = new Vector2Int[] { Vector2Int.up, Vector2Int.up + Vector2Int.right, Vector2Int.right, Vector2Int.zero };

        public int uncollapsed = 0;
        readonly WFCSlot[,] slots;
        public readonly WeightedRandomSet<Vector2Int> entropyQueue = new();
        readonly BitArray passages;
        readonly WFCTile[,] tiles;

        public Vector2Int lastCollapsedSlot;
        public (int module, int height) lastCollapsedTo;
        int _id;
        public WFCState()
        {
            slots = new WFCSlot[WorldUtils.WORLD_SIZE.x + 1, WorldUtils.WORLD_SIZE.y + 1];
            passages = new BitArray((WorldUtils.WORLD_SIZE.x + 1) * (WorldUtils.WORLD_SIZE.y + 1) * 4, true);
            tiles = new WFCTile[WorldUtils.WORLD_SIZE.x + 2, WorldUtils.WORLD_SIZE.y + 2];
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    tiles[x, y] = new(true);
                }
            }
            _id = 0;
        }
        public WFCState(WFCState original)
        {
            uncollapsed = original.uncollapsed;
            slots = (WFCSlot[,])original.slots.Clone();
            entropyQueue = new(original.entropyQueue);
            passages = new(original.passages);
            tiles = (WFCTile[,])original.tiles.Clone();
            _id = original._id + 1000000;
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

        public void CollapseRandom(ref RandomSet<WFCSlot> dirty)
        {
            _id++;
            Vector2Int pos = entropyQueue.PopRandom();
            WFCSlot s = slots[pos.x, pos.y];
            lastCollapsedSlot = s.pos;
            s = s.Collapse(this);
            HashSet<Vector2Int> updated = s.UpdateConstraints(this);
            WFCGenerator.MarkNeighborsDirty(s.pos, updated, this, ref dirty);
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
        public (bool passable, bool unpassable) GetValidPassagesAt(int x, int y, bool vertical)
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
        public void SetValidPassagesAt(int x, int y, bool vertical, (bool passable, bool unpassable) p)
        {
            if (!WorldUtils.IsInRange(x, y, slots.GetLength(0), slots.GetLength(1)))
                return;
            int pos = x + y * (WorldUtils.WORLD_SIZE.x + 1) + (vertical ? 0 : (WorldUtils.WORLD_SIZE.x + 1) * (WorldUtils.WORLD_SIZE.y + 1));
            passages[pos * 2] = p.passable;
            passages[pos * 2 + 1] = p.unpassable;
        }
        //TILES
        public WFCTile[] GetVaildTilesAtSlot(Vector2Int pos)
        {
            WFCTile[] ret = new WFCTile[4];
            for (int i = 0; i < 4; i++)
            {
                Vector2Int tilePos = pos + slotToTileArrayOffsets[i];
                ret[i] = GetTile(tilePos);
            }
            return ret;
        }
        public void SetValidTilesAtSlot(Vector2Int pos, WFCTile[] newTiles)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector2Int tilePos = pos + slotToTileArrayOffsets[i];
                SetTile(tilePos, newTiles[i]);
            }
        }
        public WFCTile GetTile(Vector2Int pos)
        {
            return tiles[pos.x, pos.y];
        }
        public void SetTile(Vector2Int pos, WFCTile tile)
        {
            tiles[pos.x, pos.y] = tile;
        }

        public override string ToString()
        {
            return $"({_id}) {base.ToString()}";
        }
    }
}