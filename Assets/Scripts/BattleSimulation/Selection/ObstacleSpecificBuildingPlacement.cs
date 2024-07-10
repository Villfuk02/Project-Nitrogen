using System.Linq;
using BattleSimulation.World;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class ObstacleSpecificBuildingPlacement : BuildingPlacement
    {
        [Header("Settings")]
        [SerializeField] Tile.Obstacle[] obstacleTypes = { Tile.Obstacle.None };
        [SerializeField] bool onSlants;

        public override bool IsTileValid(Tile? tile)
        {
            if (tile == null || tile.Building != null || !obstacleTypes.Contains(tile.obstacle))
                return false;
            if (!onSlants && tile.slant != WorldUtils.Slant.None)
                return false;
            return true;
        }
    }
}