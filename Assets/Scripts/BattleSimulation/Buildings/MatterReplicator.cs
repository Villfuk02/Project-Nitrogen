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
            Blueprint.materialProduction += productionIncrease;
            BattleController.updateMaterialsPerWave.Invoke(0);
        }
    }
}