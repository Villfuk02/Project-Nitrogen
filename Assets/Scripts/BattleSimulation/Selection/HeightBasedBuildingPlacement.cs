using BattleSimulation.World;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class HeightBasedBuildingPlacement : BuildingPlacement
    {
        [Header("Settings")]
        [SerializeField] bool onSlants;
        [SerializeField] float minHeight;
        [SerializeField] float maxHeight;

        public override bool IsTileValid(Tile? tile)
        {
            if (tile == null || tile.Building != null || tile.obstacle != Tile.Obstacle.None)
                return false;
            if (!onSlants && tile.slant != WorldUtils.Slant.None)
                return false;
            if (tile.height < minHeight || tile.height > maxHeight)
                return false;
            return true;
        }
    }
}