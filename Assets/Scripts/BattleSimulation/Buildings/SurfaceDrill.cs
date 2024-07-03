using System.Collections.Generic;
using System.Text;
using BattleSimulation.Control;
using UnityEngine;
using Utils;

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
            base.OnPlaced();
            if (onFuelTile)
            {
                WaveController.ON_WAVE_FINISHED.RegisterReaction(ProduceFuel, 100);
                PlayerState.FUEL_PER_WAVE.RegisterModifier(ProvideFuelIncome, -100);
            }
            else
            {
                WaveController.ON_WAVE_FINISHED.RegisterReaction(ProduceMaterials, 100);
                PlayerState.MATERIALS_PER_WAVE.RegisterModifier(ProvideMaterialsIncome, -100);
            }
        }

        protected override void OnDestroy()
        {
            if (Placed)
            {
                if (onFuelTile)
                {
                    WaveController.ON_WAVE_FINISHED.UnregisterReaction(ProduceFuel);
                    PlayerState.FUEL_PER_WAVE.UnregisterModifier(ProvideFuelIncome);
                }
                else
                {
                    WaveController.ON_WAVE_FINISHED.UnregisterReaction(ProduceMaterials);
                    PlayerState.MATERIALS_PER_WAVE.UnregisterModifier(ProvideMaterialsIncome);
                }
            }

            base.OnDestroy();
        }

        void ProduceFuel()
        {
            (object, float amt) data = (this, currentBlueprint.fuelProduction);
            if (PlayerState.ADD_FUEL.InvokeRef(ref data))
                fuelProduced += (int)data.amt;
        }

        void ProduceMaterials()
        {
            (object, float amt) data = (this, currentBlueprint.materialProduction);
            if (PlayerState.ADD_MATERIAL.InvokeRef(ref data))
                materialsProduced += (int)data.amt;
        }

        void ProvideMaterialsIncome(Unit _, ref float income)
        {
            income += currentBlueprint.materialProduction;
        }

        void ProvideFuelIncome(Unit _, ref float income)
        {
            income += currentBlueprint.fuelProduction;
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