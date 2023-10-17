using BattleSimulation.World;
using System;
using System.Linq;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class SpacedBuildingPlacement : BuildingPlacement
    {
        [SerializeField] bool onSlants;

        public override bool IsTileValid(Tile tile)
        {
            if (tile == null || tile.building != null || tile.obstacle != Tile.Obstacle.None)
                return false;
            if (!onSlants && tile.slant != WorldUtils.Slant.None)
                return false;
            Type t = b.GetType();
            if (WorldUtils.ADJACENT_DIRS.Any(d => HasBuildingType(t, tile.pos + d)))
                return false;
            return true;
        }

        bool HasBuildingType(Type type, Vector2Int tilePos)
        {
            if (!Tiles.TILES.TryGet(tilePos, out Tile t))
                return false;
            if (t.building == null)
                return false;
            return ReferenceEquals(t.building.GetType(), type);
        }
    }
}
