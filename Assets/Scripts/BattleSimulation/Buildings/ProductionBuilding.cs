using BattleSimulation.Control;

namespace BattleSimulation.Buildings
{
    public class ProductionBuilding : Building
    {
        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.Register(Produce, 100);
            BattleController.updateMaterialsPerWave.Register(ProvideMaterialsIncome, -100);
            BattleController.updateEnergyPerWave.Register(ProvideEnergyIncome, -100);
            BattleController.updateFuelPerWave.Register(ProvideFuelIncome, -100);

            BattleController.updateMaterialsPerWave.Invoke(0);
            BattleController.updateEnergyPerWave.Invoke(0);
            BattleController.updateFuelPerWave.Invoke(0);
        }

        protected override void OnDestroy()
        {
            if (placed)
            {
                WaveController.onWaveFinished.Unregister(Produce);
                BattleController.updateMaterialsPerWave.Unregister(ProvideMaterialsIncome);
                BattleController.updateEnergyPerWave.Unregister(ProvideEnergyIncome);
                BattleController.updateFuelPerWave.Unregister(ProvideFuelIncome);
            }

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

        bool ProvideMaterialsIncome(ref float income)
        {
            if (Blueprint.HasMaterialGeneration)
                income += Blueprint.materialGeneration;
            return true;
        }
        bool ProvideEnergyIncome(ref float income)
        {
            if (Blueprint.HasEnergyGeneration)
                income += Blueprint.energyGeneration;
            return true;
        }
        bool ProvideFuelIncome(ref float income)
        {
            if (Blueprint.HasFuelGeneration)
                income += Blueprint.fuelGeneration;
            return true;
        }
    }
}
