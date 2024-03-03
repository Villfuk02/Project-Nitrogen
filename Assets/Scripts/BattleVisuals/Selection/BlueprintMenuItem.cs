using BattleSimulation.Control;
using Game.Blueprint;
using System;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class BlueprintMenuItem : MonoBehaviour
    {
        public BlueprintDisplay display;
        [SerializeField] Color unreadyHighlightColor;
        [SerializeField] Color readyHighlightColor;
        [SerializeField] Color selectedHighlightColor;
        [SerializeField] Color invalidHighlightColor;
        [SerializeField] Color readyTextColor;
        [SerializeField] Color cantAffordTextColor;
        [SerializeField] Color useMaterialsTextColor;
        [SerializeField] Color onCooldownTextColor;

        public void UpdateItem(int cooldown, bool waveStarted, bool selected)
        {
            display.selected = selected;

            display.targetCooldownFill = cooldown / (float)Mathf.Max(display.blueprint.cooldown, 1);

            var (affordable, energy, materials) = BattleController.canAfford.Query((display.blueprint.energyCost, display.blueprint.materialCost));

            display.UpdateText(energy, materials, GetTextColor(cooldown > 0, affordable));

            bool ready = cooldown == 0 && affordable != BattleController.Affordable.No && display.blueprint.type == Blueprint.Type.Ability == waveStarted;
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
