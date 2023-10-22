using BattleSimulation.Control;

namespace BattleSimulation.Buildings
{
    public class ProductionBuilding : Building
    {
        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.Register(100, Produce);
        }

        protected override void OnDestroy()
        {
            if (placed)
                WaveController.onWaveFinished.Unregister(Produce);

            base.OnDestroy();
        }

        void Produce()
        {
            if (Blueprint.HasMaterialGeneration)
            {
                (object source, int amount) material = (this, Blueprint.materialGeneration);
                BattleController.addMaterial.Invoke(ref material);
            }

            if (Blueprint.HasEnergyGeneration)
            {
                (object source, int amount) energy = (this, Blueprint.energyGeneration);
                BattleController.addEnergy.Invoke(ref energy);
            }

            if (Blueprint.HasFuelGeneration)
            {
                (object source, int amount) fuel = (this, Blueprint.fuelGeneration);
                BattleController.addFuel.Invoke(ref fuel);
            }
        }
    }
}
