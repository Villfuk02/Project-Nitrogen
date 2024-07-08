using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public class MatterReplicator : ProductionBuilding
    {
        [Header("Matter Replicator")]
        [SerializeField] int productionIncrease;
        [SerializeField] int totalIncrease;

        static MatterReplicator()
        {
            Blueprint.MaterialProduction.RegisterModifier(UpdateProduction, -1000000);
        }

        protected override void Produce()
        {
            base.Produce();
            totalIncrease += productionIncrease;
        }

        static void UpdateProduction(IBlueprintProvider provider, ref float production)
        {
            if (provider is MatterReplicator m)
                production += m.totalIncrease;
        }
    }
}