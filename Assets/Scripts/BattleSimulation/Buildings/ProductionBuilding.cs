using BattleSimulation.Control;
using System.Text;

namespace BattleSimulation.Buildings
{
    public class ProductionBuilding : Building
    {
        protected int fuelProduced;
        protected int materialsProduced;
        protected int energyProduced;
        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.RegisterReaction(Produce, 100);

            BattleController.updateMaterialsPerWave.RegisterModifier(ProvideMaterialsIncome, -100);
            BattleController.updateEnergyPerWave.RegisterModifier(ProvideEnergyIncome, -100);
            BattleController.updateFuelPerWave.RegisterModifier(ProvideFuelIncome, -100);

            BattleController.updateMaterialsPerWave.Invoke(0);
            BattleController.updateEnergyPerWave.Invoke(0);
            BattleController.updateFuelPerWave.Invoke(0);
        }

        protected override void OnDestroy()
        {
            if (placed)
            {
                WaveController.onWaveFinished.UnregisterReaction(Produce);
                BattleController.updateMaterialsPerWave.UnregisterModifier(ProvideMaterialsIncome);
                BattleController.updateEnergyPerWave.UnregisterModifier(ProvideEnergyIncome);
                BattleController.updateFuelPerWave.UnregisterModifier(ProvideFuelIncome);
            }

            base.OnDestroy();
        }

        void Produce()
        {
            if (Blueprint.HasFuelProduction)
            {
                (object, float amt) data = (this, Blueprint.fuelProduction);
                BattleController.addFuel.InvokeRef(ref data);
                fuelProduced += (int)data.amt;
            }

            if (Blueprint.HasMaterialProduction)
            {
                (object, float amt) data = (this, Blueprint.materialProduction);
                BattleController.addMaterial.InvokeRef(ref data);
                materialsProduced += (int)data.amt;
            }

            if (Blueprint.HasEnergyProduction)
            {
                (object, float amt) data = (this, Blueprint.energyProduction);
                BattleController.addEnergy.InvokeRef(ref data);
                energyProduced += (int)data.amt;
            }
        }

        bool ProvideMaterialsIncome(ref float income)
        {
            if (Blueprint.HasMaterialProduction)
                income += Blueprint.materialProduction;
            return true;
        }
        bool ProvideEnergyIncome(ref float income)
        {
            if (Blueprint.HasEnergyProduction)
                income += Blueprint.energyProduction;
            return true;
        }
        bool ProvideFuelIncome(ref float income)
        {
            if (Blueprint.HasFuelProduction)
                income += Blueprint.fuelProduction;
            return true;
        }

        public override string? GetExtraStats()
        {
            if (fuelProduced == 0 && materialsProduced == 0 && energyProduced == 0)
                return null;
            StringBuilder sb = new();
            sb.Append("Produced");
            if (fuelProduced > 0)
                sb.Append($" [#FUE]{fuelProduced}");
            if (materialsProduced > 0)
                sb.Append($" [#MAT]{materialsProduced}");
            if (energyProduced > 0)
                sb.Append($" [#ENE]{energyProduced}");
            return sb.ToString();
        }
    }
}
