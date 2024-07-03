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
            PlayerState.ENERGY_PER_WAVE.RegisterModifier(ProvideEnergyIncome, -100);
        }

        protected override void OnDestroy()
        {
            if (Placed)
            {
                WaveController.ON_WAVE_FINISHED.UnregisterReaction(Produce);
                PlayerState.ENERGY_PER_WAVE.UnregisterModifier(ProvideEnergyIncome);
            }

            base.OnDestroy();
        }

        public void UpdateProduction(int height)
        {
            production = height * currentBlueprint.energyProduction;
        }

        void Produce()
        {
            (object, float amt) data = (this, production);
            if (PlayerState.ADD_ENERGY.InvokeRef(ref data))
                energyProduced += (int)data.amt;
        }

        void ProvideEnergyIncome(Unit _, ref float income)
        {
            income += production;
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

            yield return $"Production [#+]{TextUtils.FormatIntStat(TextUtils.Icon.Energy, production, baseBlueprint.energyProduction, TextUtils.Improvement.More)}";

            foreach (string s in base.GetExtraStats())
                yield return s;
        }
    }
}