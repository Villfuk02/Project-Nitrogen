using BattleSimulation.Control;
using Game.Blueprint;
using System.Linq;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public class BlueprintMenu : MonoBehaviour
    {
        [Header("Settings")]
        public Blueprint[] originalBlueprints;
        [Header("Runtime variables")]
        public Blueprint[] blueprints;
        public int[] cooldowns;
        public int selected;
        public bool waveStarted;

        void Awake()
        {
            blueprints = originalBlueprints.Select(b => b.Clone()).ToArray();
            cooldowns = blueprints.Select(b => b.startingCooldown).ToArray();

            WaveController.onWaveFinished.RegisterReaction(OnWaveFinished, 10);
            WaveController.startWave.RegisterReaction(OnWaveStarted, 10);
        }

        void OnDestroy()
        {
            WaveController.onWaveFinished.UnregisterReaction(OnWaveFinished);
            WaveController.startWave.UnregisterReaction(OnWaveStarted);
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

        public bool TryPlace()
        {
            if (blueprints[selected].type == Blueprint.Type.Ability != waveStarted)
                return false;
            if (cooldowns[selected] > 0)
                return false;
            if (!BattleController.AdjustAndTrySpend(blueprints[selected].energyCost, blueprints[selected].materialCost))
                return false;
            cooldowns[selected] = blueprints[selected].cooldown;
            return true;
        }

        public void OnWaveStarted()
        {
            waveStarted = true;
        }

        public void OnWaveFinished()
        {
            waveStarted = false;
            ReduceCooldowns();
        }

        public void ReduceCooldowns()
        {
            for (int i = 0; i < cooldowns.Length; i++)
                if (cooldowns[i] > 0)
                    cooldowns[i]--;
        }

        void FinishCooldowns()
        {
            for (int i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = 0;
        }
    }
}
