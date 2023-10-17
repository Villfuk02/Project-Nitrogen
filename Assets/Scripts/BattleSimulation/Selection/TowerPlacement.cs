using BattleSimulation.World;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class TowerPlacement : BuildingPlacement
    {
        [SerializeField] bool onSlants;

        public override bool IsTileValid(Tile tile)
        {
            if (tile == null || tile.building != null || tile.obstacle != Tile.Obstacle.None)
                return false;
            if (!onSlants && tile.slant != WorldUtils.Slant.None)
                return false;
            return true;
        }
    }
}
