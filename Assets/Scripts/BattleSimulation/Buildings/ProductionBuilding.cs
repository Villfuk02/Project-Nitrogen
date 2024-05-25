using System.Collections.Generic;
using System.Text;
using BattleSimulation.Control;

namespace BattleSimulation.Buildings
{
    public class ProductionBuilding : Building
    {
        protected int fuelProduced;
        protected int materialsProduced;
        protected int energyProduced;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            WaveController.ON_WAVE_FINISHED.RegisterReaction(Produce, 100);

            BattleController.UPDATE_MATERIALS_PER_WAVE.RegisterModifier(ProvideMaterialsIncome, -100);
            BattleController.UPDATE_ENERGY_PER_WAVE.RegisterModifier(ProvideEnergyIncome, -100);
            BattleController.UPDATE_FUEL_PER_WAVE.RegisterModifier(ProvideFuelIncome, -100);

            BattleController.UPDATE_MATERIALS_PER_WAVE.Invoke(0);
            BattleController.UPDATE_ENERGY_PER_WAVE.Invoke(0);
            BattleController.UPDATE_FUEL_PER_WAVE.Invoke(0);
        }

        protected override void OnDestroy()
        {
            if (Placed)
            {
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(Produce);
                BattleController.UPDATE_MATERIALS_PER_WAVE.UnregisterModifier(ProvideMaterialsIncome);
                BattleController.UPDATE_ENERGY_PER_WAVE.UnregisterModifier(ProvideEnergyIncome);
                BattleController.UPDATE_FUEL_PER_WAVE.UnregisterModifier(ProvideFuelIncome);
            }

            base.OnDestroy();
        }

        protected virtual void Produce()
        {
            if (Blueprint.HasFuelProduction)
            {
                (object, float amt) data = (this, Blueprint.fuelProduction);
                if (BattleController.ADD_FUEL.InvokeRef(ref data))
                    fuelProduced += (int)data.amt;
            }

            if (Blueprint.HasMaterialProduction)
            {
                (object, float amt) data = (this, Blueprint.materialProduction);
                if (BattleController.ADD_MATERIAL.InvokeRef(ref data))
                    materialsProduced += (int)data.amt;
            }

            if (Blueprint.HasEnergyProduction)
            {
                (object, float amt) data = (this, Blueprint.energyProduction);
                if (BattleController.ADD_ENERGY.InvokeRef(ref data))
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

        public override IEnumerable<string> GetExtraStats()
        {
            if (fuelProduced != 0 || materialsProduced != 0 || energyProduced != 0)
            {
                StringBuilder sb = new();
                sb.Append("Produced");
                if (fuelProduced > 0)
                    sb.Append($" [#FUE]{fuelProduced}");
                if (materialsProduced > 0)
                    sb.Append($" [#MAT]{materialsProduced}");
                if (energyProduced > 0)
                    sb.Append($" [#ENE]{energyProduced}");
                yield return sb.ToString();
            }

            foreach (string s in base.GetExtraStats())
                yield return s;
        }
    }
}