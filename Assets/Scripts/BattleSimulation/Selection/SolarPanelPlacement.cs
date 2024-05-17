using BattleSimulation.Buildings;
using BattleSimulation.World;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public class SolarPanelPlacement : HeightBasedBuildingPlacement
    {
        public override bool Setup(Selectable selected, int rotation, Vector3? pos, Transform defaultParent)
        {
            ((SolarPanel)b).UpdateProduction(selected?.tile is Tile t && t != null ? Mathf.FloorToInt(t.height) : 0);
            return base.Setup(selected, rotation, pos, defaultParent);
        }
    }
}