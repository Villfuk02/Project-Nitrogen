using BattleSimulation.Control;
using BattleSimulation.Selection;
using Game.Blueprint;
using System;
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
            display.InitBlueprint(entry.current);
            this.entry = entry;
        }

        public void UpdateItem(int selectedIndex)
        {
            bool selected = entry.index == selectedIndex;

            display.selected = selected;

            display.targetCooldownFill = entry.cooldown / (float)Mathf.Max(display.blueprint.cooldown, 1);

            var (affordable, _, _) = BattleController.canAfford.Query((display.blueprint.energyCost, display.blueprint.materialCost));

            display.UpdateText(display.blueprint.energyCost, display.blueprint.materialCost, GetTextColor(entry.cooldown > 0, affordable));

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
