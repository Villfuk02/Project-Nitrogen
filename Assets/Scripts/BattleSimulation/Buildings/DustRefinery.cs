namespace BattleSimulation.Buildings
{
    public class DustRefinery : ProductionBuilding
    {
        protected override void OnPlaced()
        {
            OriginalBlueprint.materialCost += OriginalBlueprint.magic1;
            base.OnPlaced();
        }
    }
}
