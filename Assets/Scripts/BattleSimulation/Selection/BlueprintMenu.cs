using System;
using System.Collections.Generic;
using System.Linq;
using BattleSimulation.Control;
using Game.Blueprint;
using Game.Run;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class BlueprintMenu : MonoBehaviour, IBlueprintHolder
    {
        public class MenuEntry
        {
            public readonly Blueprint blueprint;
            public readonly int index;
            public int cooldown;

            public MenuEntry(Blueprint original, int index)
            {
                blueprint = original.Clone();
                cooldown = blueprint.startingCooldown;
                this.index = index;
            }
        }

        [Header("References")]
        [SerializeField] SelectionController selectionController;
        [Header("Runtime variables")]
        public int selected;
        public bool waveStarted;
        public List<MenuEntry> abilities;
        public List<MenuEntry> buildings;
        List<MenuEntry> CurrentEntries => waveStarted ? abilities : buildings;

        [Header("Cheats")]
        [SerializeField] bool cheatFinishCooldowns;

        void Awake()
        {
            RunPersistence rp = GameObject.FindGameObjectWithTag(TagNames.RUN_PERSISTENCE).GetComponent<RunPersistence>();
            abilities = rp.blueprints.Where(b => b.type == Blueprint.Type.Ability).Select((b, i) => new MenuEntry(b, i)).ToList();
            buildings = rp.blueprints.Where(b => b.type != Blueprint.Type.Ability).Select((b, i) => new MenuEntry(b, i)).ToList();

            WaveController.ON_WAVE_FINISHED.RegisterReaction(OnWaveFinished, 10);
            WaveController.START_WAVE.RegisterReaction(OnWaveStarted, 10);
        }

        void OnDestroy()
        {
            WaveController.ON_WAVE_FINISHED.UnregisterReaction(OnWaveFinished);
            WaveController.START_WAVE.UnregisterReaction(OnWaveStarted);
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

        public bool TryGetBlueprints(int index, out Blueprint blueprint, out Func<int>? cooldown)
        {
            blueprint = null;
            cooldown = null;

            if (index < 0 || index >= CurrentEntries.Count)
                return false;

            MenuEntry entry = CurrentEntries[index];
            blueprint = entry.blueprint;
            cooldown = () => entry.cooldown;
            return true;
        }

        public bool TrySelect(int index, out Blueprint blueprint, out Func<int> cooldown)
        {
            if (!TryGetBlueprints(index, out blueprint, out cooldown))
                return false;
            selected = index;
            return true;
        }

        public bool TryPlace()
        {
            if (CurrentEntries[selected].cooldown > 0)
                return false;
            var blueprint = CurrentEntries[selected].blueprint;
            if (!PlayerState.AdjustAndTrySpend(Blueprint.EnergyCost.Query(blueprint), Blueprint.MaterialCost.Query(blueprint)))
                return false;
            CurrentEntries[selected].cooldown = Blueprint.Cooldown.Query(blueprint);
            return true;
        }

        public void OnWaveStarted()
        {
            waveStarted = true;
            if (selected != -1)
                selectionController.DeselectFromMenu();
        }

        public void OnWaveFinished()
        {
            waveStarted = false;
            if (selected != -1)
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

        public IEnumerable<Blueprint> GetBlueprints()
        {
            return abilities.Concat(buildings).Select(e => e.blueprint);
        }
    }
}