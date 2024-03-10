using BattleSimulation.World;
using System.Linq;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class SimpleBuildingPlacement : BuildingPlacement
    {
        [Header("Settings")]
        [SerializeField] bool onSlants;
        [SerializeField] Tile.Obstacle[] allowedObstacles;

        public override bool IsTileValid(Tile tile)
        {
            if (tile == null || tile.Building != null || !allowedObstacles.Contains(tile.obstacle))
                return false;
            if (!onSlants && tile.slant != WorldUtils.Slant.None)
                return false;
            return true;
        }
    }
}
