using BattleSimulation.Control;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public class SurfaceDrill : Building
    {
        public bool onFuelTile;
        [Header("Runtime variables")]
        [SerializeField] int fuelProduced;
        [SerializeField] int materialsProduced;

        protected override void OnPlaced()
        {
            if (onFuelTile)
            {
                WaveController.onWaveFinished.RegisterReaction(ProduceFuel, 100);
                BattleController.updateFuelPerWave.RegisterModifier(ProvideFuelIncome, -100);
                BattleController.updateFuelPerWave.Invoke(0);
            }
            else
            {
                WaveController.onWaveFinished.RegisterReaction(ProduceMaterials, 100);
                BattleController.updateMaterialsPerWave.RegisterModifier(ProvideMaterialsIncome, -100);
                BattleController.updateMaterialsPerWave.Invoke(0);
            }
        }

        protected override void OnDestroy()
        {
            if (Placed)
            {
                if (onFuelTile)
                {
                    WaveController.onWaveFinished.UnregisterReaction(ProduceFuel);
                    BattleController.updateFuelPerWave.UnregisterModifier(ProvideFuelIncome);
                }
                else
                {
                    WaveController.onWaveFinished.UnregisterReaction(ProduceMaterials);
                    BattleController.updateMaterialsPerWave.UnregisterModifier(ProvideMaterialsIncome);
                }
            }

            base.OnDestroy();
        }

        void ProduceFuel()
        {
            (object, float amt) data = (this, Blueprint.fuelProduction);
            if (BattleController.addFuel.InvokeRef(ref data))
                fuelProduced += (int)data.amt;
        }

        void ProduceMaterials()
        {
            (object, float amt) data = (this, Blueprint.materialProduction);
            if (BattleController.addMaterial.InvokeRef(ref data))
                    materialsProduced += (int)data.amt;
        }

        bool ProvideMaterialsIncome(ref float income)
        {
            income += Blueprint.materialProduction;
            return true;
        }


        bool ProvideFuelIncome(ref float income)
        {
            income += Blueprint.fuelProduction;
            return true;
        }

        public override IEnumerable<string> GetExtraStats()
        {
            if (fuelProduced != 0 || materialsProduced != 0)
            {
                StringBuilder sb = new();
                sb.Append("Produced");
                if (fuelProduced > 0)
                    sb.Append($" [#FUE]{fuelProduced}");
                if (materialsProduced > 0)
                    sb.Append($" [#MAT]{materialsProduced}");
                yield return sb.ToString();
            }

            foreach (string s in base.GetExtraStats())
                yield return s;
        }
    }
}
