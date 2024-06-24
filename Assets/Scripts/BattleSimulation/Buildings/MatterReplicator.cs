using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public class MatterReplicator : ProductionBuilding
    {
        [Header("Settings")]
        [SerializeField] int productionIncrease;
        [SerializeField] int totalIncrease;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            Blueprint.MaterialProduction.RegisterModifier(UpdateProduction, -1000000);
        }

        protected override void OnDestroy()
        {
            if (Placed)
                Blueprint.MaterialProduction.UnregisterModifier(UpdateProduction);

            base.OnDestroy();
        }

        protected override void Produce()
        {
            base.Produce();
            totalIncrease += productionIncrease;
        }

        void UpdateProduction(IBlueprintProvider provider, ref float production)
        {
            if (provider as Blueprinted == this)
                production += totalIncrease;
        }
    }
}