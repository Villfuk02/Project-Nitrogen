using BattleSimulation.Control;

namespace BattleSimulation.Buildings
{
    public class SurfaceDrill : Building
    {
        protected override void OnPlaced()
        {
            WaveController.onWaveFinished.Register(100, Produce);
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
