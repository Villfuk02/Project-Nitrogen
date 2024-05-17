using BattleSimulation.Buildings;
using BattleSimulation.World;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public class SurfaceDrillPlacement : ObstacleSpecificBuildingPlacement
    {
        public override bool Setup(Selectable selected, int rotation, Vector3? pos, Transform defaultParent)
        {
            if (selected != null && selected.tile != null)
                ((SurfaceDrill)b).onFuelTile = selected.tile.obstacle == Tile.Obstacle.Fuel;
            return base.Setup(selected, rotation, pos, defaultParent);
        }
    }
}
