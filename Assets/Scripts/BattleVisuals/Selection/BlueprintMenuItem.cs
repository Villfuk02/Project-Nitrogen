using System;
using BattleSimulation.Control;
using BattleSimulation.Selection;
using Game.Blueprint;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class BlueprintMenuItem : MonoBehaviour
    {
        [Header("References")]
        public BlueprintDisplay display;
        [Header("Settings")]
        [SerializeField] Color unreadyHighlightColor;
        [SerializeField] Color readyHighlightColor;
        [SerializeField] Color selectedHighlightColor;
        [SerializeField] Color invalidHighlightColor;
        [SerializeField] Color readyTextColor;
        [SerializeField] Color cantAffordTextColor;
        [SerializeField] Color useMaterialsTextColor;
        [SerializeField] Color onCooldownTextColor;
        [Header("Runtime variables")]
        [SerializeField] BlueprintMenu.MenuEntry entry;

        public void Init(BlueprintMenu.MenuEntry entry)
        {
            display.InitBlueprint(entry.blueprint);
            this.entry = entry;
        }

        public void UpdateItem(int selectedIndex)
        {
            bool selected = entry.index == selectedIndex;

            display.selected = selected;

            display.targetCooldownFill = entry.cooldown / (float)Mathf.Max(display.blueprint.cooldown, 1);

            int energy = Blueprint.EnergyCost.Query(display.blueprint);
            int materials = Blueprint.MaterialCost.Query(display.blueprint);
            var (affordable, _, _) = BattleController.CAN_AFFORD.Query((energy, materials));

            display.UpdateText(energy, materials, GetTextColor(entry.cooldown > 0, affordable));

            bool ready = entry.cooldown == 0 && affordable != BattleController.Affordable.No;
            display.highlight.color = GetHighlightColor(ready, selected);
        }

        Color GetTextColor(bool onCooldown, BattleController.Affordable affordable)
        {
            Color c = affordable switch
            {
                BattleController.Affordable.Yes => readyTextColor,
                BattleController.Affordable.UseMaterialsAsEnergy => useMaterialsTextColor,
                BattleController.Affordable.No => cantAffordTextColor,
                _ => throw new ArgumentOutOfRangeException(nameof(affordable), affordable, null)
            };
            if (onCooldown && affordable != BattleController.Affordable.No)
                return c * onCooldownTextColor;
            return c;
        }

        Color GetHighlightColor(bool ready, bool selected) => (ready, selected) switch
        {
            (true, true) => selectedHighlightColor,
            (false, true) => invalidHighlightColor,
            (true, false) => readyHighlightColor,
            (false, false) => unreadyHighlightColor
        };
    }
}