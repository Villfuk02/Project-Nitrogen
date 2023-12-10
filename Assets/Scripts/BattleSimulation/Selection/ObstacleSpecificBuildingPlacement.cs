using BattleSimulation.World;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class ObstacleSpecificBuildingPlacement : BuildingPlacement
    {
        [SerializeField] Tile.Obstacle obstacleType;
        [SerializeField] bool onSlants;

        public override bool IsTileValid(Tile tile)
        {
            if (tile == null || tile.Building != null || tile.obstacle != obstacleType)
                return false;
            if (!onSlants && tile.slant != WorldUtils.Slant.None)
                return false;
            return true;
        }
    }
}
