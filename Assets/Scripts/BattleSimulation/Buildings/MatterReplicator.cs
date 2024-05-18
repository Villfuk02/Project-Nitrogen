using BattleSimulation.Control;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public class MatterReplicator : ProductionBuilding
    {
        [Header("Settings")]
        [SerializeField] int productionIncrease;

        protected override void Produce()
        {
            base.Produce();
            baseBlueprint.materialProduction += productionIncrease;
            BattleController.UPDATE_MATERIALS_PER_WAVE.Invoke(0);
        }
    }
}