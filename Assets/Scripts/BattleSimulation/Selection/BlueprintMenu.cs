using BattleSimulation.Control;
using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public class BlueprintMenu : MonoBehaviour
    {
        public Blueprint[] originalBlueprints;
        public Blueprint[] blueprints;
        public int[] cooldowns;
        public int selected;
        public bool waveStarted;

        void Awake()
        {
            blueprints = new Blueprint[originalBlueprints.Length];
            cooldowns = new int[blueprints.Length];
            for (int i = 0; i < blueprints.Length; i++)
            {
                blueprints[i] = originalBlueprints[i].Clone();
                cooldowns[i] = blueprints[i].startingCooldown;
            }
            WaveController.onWaveFinished.Register(WaveFinished, 10);
            WaveController.startWave.Register(WaveStarted, 10);
        }

        void OnDestroy()
        {
            WaveController.onWaveFinished.Unregister(WaveFinished);
            WaveController.startWave.Unregister(WaveStarted);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
                FinishCooldowns();
        }

        public void Deselect()
        {
            selected = -1;
        }

        public bool TrySelect(int index, out Blueprint blueprint, out Blueprint original)
        {
            blueprint = null;
            original = null;
            if (index < 0 || index >= blueprints.Length)
                return false;

            blueprint = blueprints[index];
            original = originalBlueprints[index];
            selected = index;
            return true;
        }

        public bool OnPlace()
        {
            if (blueprints[selected].type != Blueprint.Type.Ability && waveStarted)
                return false;
            if (cooldowns[selected] > 0)
                return false;
            if (!BattleController.AdjustAndTrySpend(blueprints[selected].energyCost, blueprints[selected].materialCost))
                return false;
            cooldowns[selected] = blueprints[selected].cooldown;
            return true;
        }

        public bool WaveStarted()
        {
            waveStarted = true;
            return true;
        }

        public void WaveFinished()
        {
            waveStarted = false;
            ReduceCooldowns();
        }

        public void ReduceCooldowns()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                if (cooldowns[i] > 0)
                    cooldowns[i]--;
            }
        }

        void FinishCooldowns()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i] = 0;
            }
        }
    }
}
