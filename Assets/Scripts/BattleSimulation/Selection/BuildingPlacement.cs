using BattleSimulation.Buildings;

namespace BattleSimulation.Selection
{
    public abstract class BuildingPlacement : TilePlacement
    {
        public override void Place()
        {
            selectedTile.Building = (Building)blueprinted;
            base.Place();
        }
    }
}