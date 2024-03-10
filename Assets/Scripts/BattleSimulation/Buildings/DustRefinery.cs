namespace BattleSimulation.Buildings
{
    public class DustRefinery : ProductionBuilding
    {
        protected override void OnPlaced()
        {
            base.OnPlaced();
            OriginalBlueprint.materialCost += OriginalBlueprint.magic1;
        }
    }
}
