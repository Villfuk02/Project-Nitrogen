using UnityEngine;

namespace BattleSimulation.Buildings
{
    public class DustRefinery : ProductionBuilding
    {
        [Header("Settings")]
        [SerializeField] int costIncrease;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            originalBlueprint.materialCost += costIncrease;
        }
    }
}