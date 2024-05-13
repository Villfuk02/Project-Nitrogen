using UnityEngine;
using Utils;

namespace BattleSimulation.Buildings
{
    public class DustRefinery : ProductionBuilding
    {
        [Header("Settings")]
        [SerializeField] int costIncrease;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            OriginalBlueprint.materialCost += costIncrease;
        }
    }
}
