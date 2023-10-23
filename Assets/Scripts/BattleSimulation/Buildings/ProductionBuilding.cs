using BattleSimulation.Control;

namespace BattleSimulation.Buildings
{
    public class ProductionBuilding : Building
    {
        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.Register(Produce, 100);
        }

        protected override void OnDestroy()
        {
            if (placed)
                WaveController.onWaveFinished.Unregister(Produce);

            base.OnDestroy();
        }

        void Produce()
        {
            if (Blueprint.HasFuelGeneration)
                BattleController.addFuel.Invoke((this, Blueprint.fuelGeneration));

            if (Blueprint.HasMaterialGeneration)
                BattleController.addMaterial.Invoke((this, Blueprint.materialGeneration));

            if (Blueprint.HasEnergyGeneration)
                BattleController.addEnergy.Invoke((this, Blueprint.energyGeneration));
        }
    }
}
