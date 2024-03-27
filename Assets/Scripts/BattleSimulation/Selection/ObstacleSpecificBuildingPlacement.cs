using BattleSimulation.World;
using System.Linq;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class ObstacleSpecificBuildingPlacement : BuildingPlacement
    {
        [Header("Settings")]
        [SerializeField] Tile.Obstacle[] obstacleTypes;
        [SerializeField] bool onSlants;

        public override bool IsTileValid(Tile tile)
        {
            if (tile == null || tile.Building != null || !obstacleTypes.Contains(tile.obstacle))
                return false;
            if (!onSlants && tile.slant != WorldUtils.Slant.None)
                return false;
            return true;
        }
    }
}
