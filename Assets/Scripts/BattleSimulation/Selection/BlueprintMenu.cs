using BattleSimulation.Control;
using Game.Blueprint;
using Game.Run;
using System.Linq;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class BlueprintMenu : MonoBehaviour
    {
        public class MenuEntry
        {
            public readonly Blueprint original;
            public readonly Blueprint current;
            public readonly int index;
            public int cooldown;

            public MenuEntry(Blueprint original, int index)
            {
                this.original = original;
                current = original.Clone();
                cooldown = current.startingCooldown;
                this.index = index;
            }
        }
        [Header("References")]
        [SerializeField] SelectionController selectionController;
        [Header("Runtime variables")]
        public MenuEntry[] abilities;
        public MenuEntry[] buildings;
        public int selected;
        public bool waveStarted;
        MenuEntry[] CurrentEntries => waveStarted ? abilities : buildings;

        [Header("Cheats")]
        [SerializeField] bool cheatFinishCooldowns;

        void Awake()
        {
            RunPersistence rp = GameObject.FindGameObjectWithTag(TagNames.RUN_PERSISTENCE).GetComponent<RunPersistence>();
            abilities = rp.blueprints.Where(b => b.type == Blueprint.Type.Ability).Select((b, i) => new MenuEntry(b, i)).ToArray();
            buildings = rp.blueprints.Where(b => b.type != Blueprint.Type.Ability).Select((b, i) => new MenuEntry(b, i)).ToArray();

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
            if (cheatFinishCooldowns)
            {
                cheatFinishCooldowns = false;
                FinishCooldowns();
            }
        }

        public void Deselect()
        {
            selected = -1;
        }

        public void GetBlueprints(int index, out Blueprint blueprint, out Blueprint original)
        {
            blueprint = CurrentEntries[index].current;
            original = CurrentEntries[index].original;
        }

        public bool TrySelect(int index, out Blueprint blueprint, out Blueprint original)
        {
            blueprint = null;
            original = null;

            if (index < 0 || index >= CurrentEntries.Length)
                return false;

            GetBlueprints(index, out blueprint, out original);
            selected = index;
            return true;
        }

        public bool TryPlace()
        {
            if (CurrentEntries[selected].cooldown > 0)
                return false;
            var blueprint = CurrentEntries[selected].current;
            if (!BattleController.AdjustAndTrySpend(blueprint.energyCost, blueprint.materialCost))
                return false;
            CurrentEntries[selected].cooldown = blueprint.cooldown;
            return true;
        }

        public void OnWaveStarted()
        {
            waveStarted = true;
            selectionController.DeselectFromMenu();
        }

        public void OnWaveFinished()
        {
            waveStarted = false;
            selectionController.DeselectFromMenu();
            ReduceCooldowns();
        }

        public void ReduceCooldowns()
        {
            foreach (var entry in abilities.Concat(buildings))
                if (entry.cooldown > 0)
                    entry.cooldown--;
        }

        void FinishCooldowns()
        {
            foreach (var entry in abilities.Concat(buildings))
                entry.cooldown = 0;
        }
    }
}
