using System.Collections.Generic;
using System.Text;
using BattleSimulation.Control;
using Utils;

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

            BattleController.MATERIALS_PER_WAVE.RegisterModifier(ProvideMaterialsIncome, -100);
            BattleController.ENERGY_PER_WAVE.RegisterModifier(ProvideEnergyIncome, -100);
            BattleController.FUEL_PER_WAVE.RegisterModifier(ProvideFuelIncome, -100);
        }

        protected override void OnDestroy()
        {
            if (Placed)
            {
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(Produce);
                BattleController.MATERIALS_PER_WAVE.UnregisterModifier(ProvideMaterialsIncome);
                BattleController.ENERGY_PER_WAVE.UnregisterModifier(ProvideEnergyIncome);
                BattleController.FUEL_PER_WAVE.UnregisterModifier(ProvideFuelIncome);
            }

            base.OnDestroy();
        }

        protected virtual void Produce()
        {
            if (currentBlueprint.HasFuelProduction)
            {
                (object, float amt) data = (this, currentBlueprint.fuelProduction);
                if (BattleController.ADD_FUEL.InvokeRef(ref data))
                    fuelProduced += (int)data.amt;
            }

            if (currentBlueprint.HasMaterialProduction)
            {
                (object, float amt) data = (this, currentBlueprint.materialProduction);
                if (BattleController.ADD_MATERIAL.InvokeRef(ref data))
                    materialsProduced += (int)data.amt;
            }

            if (currentBlueprint.HasEnergyProduction)
            {
                (object, float amt) data = (this, currentBlueprint.energyProduction);
                if (BattleController.ADD_ENERGY.InvokeRef(ref data))
                    energyProduced += (int)data.amt;
            }
        }

        void ProvideMaterialsIncome(Unit _, ref float income)
        {
            if (currentBlueprint.HasMaterialProduction)
                income += currentBlueprint.materialProduction;
        }

        void ProvideEnergyIncome(Unit _, ref float income)
        {
            if (currentBlueprint.HasEnergyProduction)
                income += currentBlueprint.energyProduction;
        }

        void ProvideFuelIncome(Unit _, ref float income)
        {
            if (currentBlueprint.HasFuelProduction)
                income += currentBlueprint.fuelProduction;
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