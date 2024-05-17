using System.Collections.Generic;
using System.Text;
using BattleSimulation.Control;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public class EnergyHarvester : Building
    {
        [SerializeField] int energyProduced;

        protected override void OnPlaced()
        {
            BattleController.addMaterial.RegisterReaction(OnAddedMaterial, 1000);
        }

        protected override void OnDestroy()
        {
            if (Placed)
                BattleController.addMaterial.UnregisterReaction(OnAddedMaterial);

            base.OnDestroy();
        }

        void OnAddedMaterial((object source, float amount) param)
        {
            (object, float amt) data = (param.source, Blueprint.energyProduction * 0.01f * param.amount);
            if (BattleController.addEnergy.InvokeRef(ref data))
                energyProduced += (int)data.amt;
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

            foreach (string s in base.GetExtraStats())
                yield return s;
        }
    }
}