using System;
using System.Linq;
using BattleSimulation.World;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class SpacedBuildingPlacement : BuildingPlacement
    {
        [Header("Settings")]
        [SerializeField] bool onSlants;

        public override bool IsTileValid(Tile? tile)
        {
            if (tile == null || tile.Building != null || tile.obstacle != Tile.Obstacle.None)
                return false;
            if (!onSlants && tile.slant != WorldUtils.Slant.None)
                return false;
            Type t = blueprinted.GetType();
            if (WorldUtils.ADJACENT_DIRS.Any(d => HasBuildingType(t, tile.pos + d)))
                return false;
            return true;
        }

        bool HasBuildingType(Type type, Vector2Int tilePos)
        {
            if (!Tiles.TILES.TryGet(tilePos, out Tile t))
                return false;
            if (t.Building == null)
                return false;
            return ReferenceEquals(t.Building.GetType(), type);
        }
    }
}