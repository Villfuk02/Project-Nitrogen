using System.Collections.Generic;
using System.Text;
using BattleSimulation.Control;
using UnityEngine;
using Utils;

namespace BattleSimulation.Buildings
{
    public class SolarPanel : Building
    {
        [Header("Runtime variables")]
        [SerializeField] int production;
        [SerializeField] int energyProduced;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            WaveController.ON_WAVE_FINISHED.RegisterReaction(Produce, 100);

            BattleController.UPDATE_ENERGY_PER_WAVE.RegisterModifier(ProvideEnergyIncome, -100);

            BattleController.UPDATE_ENERGY_PER_WAVE.Invoke(0);
        }

        protected override void OnDestroy()
        {
            if (Placed)
            {
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(Produce);
                BattleController.UPDATE_ENERGY_PER_WAVE.UnregisterModifier(ProvideEnergyIncome);
            }

            base.OnDestroy();
        }

        public void UpdateProduction(int height)
        {
            production = height * Blueprint.energyProduction;
        }

        void Produce()
        {
            (object, float amt) data = (this, production);
            if (BattleController.ADD_ENERGY.InvokeRef(ref data))
                energyProduced += (int)data.amt;
        }

        bool ProvideEnergyIncome(ref float income)
        {
            income += production;
            return true;
        }

        public override IEnumerable<string> GetExtraStats()
        {
            if (energyProduced != 0)
            {
                StringBuilder sb = new();
                sb.Append("Produced");
                if (energyProduced > 0)
                    sb.Append($" [#ENE]{energyProduced}");
                yield return sb.ToString();
            }

            yield return $"Production [#+]{TextUtils.FormatIntStat(TextUtils.Icon.Energy, production, OriginalBlueprint.energyProduction, true, TextUtils.Improvement.More)}";

            foreach (string s in base.GetExtraStats())
                yield return s;
        }
    }
}