using BattleSimulation.Control;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public class DustRefinery : Building
    {
        [SerializeField] int priceIncrease;
        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.Register(100, Produce);
            OriginalBlueprint.cost += priceIncrease;
        }

        protected override void OnDestroy()
        {
            if (placed)
            {
                WaveController.onWaveFinished.Unregister(Produce);
            }
            base.OnDestroy();
        }

        void Produce()
        {
            (object source, int aamount) param = (this, Blueprint.materialGeneration);
            BattleController.addMaterial.Invoke(ref param);
        }
    }
}
