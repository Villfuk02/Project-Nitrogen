using UnityEngine;

namespace BattleSimulation.Buildings
{
    public class DustRefinery : ProductionBuilding
    {
        [SerializeField] int priceIncrease;
        protected override void OnPlaced()
        {
            OriginalBlueprint.cost += priceIncrease;
            base.OnPlaced();
        }
    }
}
