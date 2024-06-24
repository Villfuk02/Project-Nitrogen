using System.Collections.Generic;
using System.Linq;
using Game.Blueprint;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Game.Run
{
    public class BlueprintSelectionController : MonoBehaviour
    {
        public delegate void Callback(Blueprint.Blueprint? addedBlueprint, Blueprint.Blueprint? removedBlueprint);

        [Header("References")]
        [SerializeField] GameObject blueprintPrefab;
        [SerializeField] RectTransform offerHolder;
        [SerializeField] Transform inventoryHolder;
        [SerializeField] TextMeshProUGUI offerText;
        [SerializeField] TextMeshProUGUI instructions;
        [SerializeField] Button confirmButton;
        [SerializeField] Button skipButton;
        [SerializeField] InfoPanel.InfoPanel infoPanel;
        [SerializeField] ScrollRect scrollRect;
        [Header("Settings")]
        [SerializeField] Color activeColor;
        [SerializeField] Color selectedColor;
        [SerializeField] int maxBlueprints;
        [SerializeField] int maxBlueprintsOfOneKind;
        [SerializeField] float offerHeightWithScrollbar;
        [SerializeField] float offerHeightWithoutScrollbar;
        [Header("Runtime variables")]
        [SerializeField] List<BlueprintDisplay> offeredDisplays;
        [SerializeField] List<BlueprintDisplay> inventoryDisplays;
        [SerializeField] Blueprint.Blueprint? selected;
        [SerializeField] Blueprint.Blueprint? selectedFromInventory;
        [SerializeField] bool blueprintLimited;
        [SerializeField] bool buildingLimited;
        [SerializeField] bool abilityLimited;
        Callback callback_;

        void Awake()
        {
            GameObject.FindGameObjectWithTag(TagNames.RUN_PERSISTENCE).GetComponentInChildren<BlueprintRewardController>().SignalReady(this);
        }

        public void Setup(List<Blueprint.Blueprint> inventory, List<Blueprint.Blueprint> offer, string? title, string? endText, Callback callback, bool canSkip)
        {
            foreach (var display in inventoryDisplays)
                Destroy(display.gameObject);
            inventoryDisplays.Clear();
            foreach (var blueprint in inventory)
                inventoryDisplays.Add(SetupItem(blueprint, true));

            foreach (var display in offeredDisplays)
                Destroy(display.gameObject);
            offeredDisplays.Clear();
            foreach (var blueprint in offer)
                offeredDisplays.Add(SetupItem(blueprint, false));

            if (title is not null)
                offerText.text = title;
            if (endText is not null)
                skipButton.GetComponentInChildren<TextMeshProUGUI>().text = endText;

            callback_ = callback;
            skipButton.gameObject.SetActive(canSkip);

            selected = null;
            selectedFromInventory = null;

            blueprintLimited = inventory.Count >= maxBlueprints;
            buildingLimited = inventory.Count(b => b.type != Blueprint.Blueprint.Type.Ability) >= maxBlueprintsOfOneKind;
            abilityLimited = inventory.Count(b => b.type == Blueprint.Blueprint.Type.Ability) >= maxBlueprintsOfOneKind;
            UpdateInterfaceState();

            offerHolder.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, offer.Count > 6 ? offerHeightWithScrollbar : offerHeightWithoutScrollbar);
            offerHolder.anchoredPosition = Vector2.zero;
            Invoke(nameof(ResetPosition), 0.001f);
        }

        void ResetPosition() => scrollRect.horizontalNormalizedPosition = 0;

        BlueprintDisplay SetupItem(Blueprint.Blueprint blueprint, bool inventory)
        {
            var item = Instantiate(blueprintPrefab, inventory ? inventoryHolder : offerHolder).GetComponent<BlueprintDisplay>();
            item.InitBlueprint(blueprint);
            item.onClick.AddListener(() => Select(item, inventory));
            item.onHover.AddListener(() => Hover(item));
            item.onUnhover.AddListener(Unhover);
            item.highlightScale = item.highlightScale * 3 - 2;
            item.selectedHighlightScale = item.selectedHighlightScale * 2 - 1;
            return item;
        }

        public void Confirm()
        {
            infoPanel.Hide(true, true);
            callback_.Invoke(selected, selectedFromInventory);
        }

        public void Skip() => callback_.Invoke(null, null);

        public void Hover(BlueprintDisplay self)
        {
            infoPanel.ShowBlueprint(self.blueprint, null, false, false);
        }

        public void Unhover()
        {
            infoPanel.Hide(false, false);
        }

        public void Select(BlueprintDisplay self, bool inventory)
        {
            bool equalsSelected = self.blueprint == (inventory ? selectedFromInventory : selected);
            if (!equalsSelected)
            {
                if (inventory)
                {
                    if (!blueprintLimited && !buildingLimited && !abilityLimited)
                        return;
                    if (!ValidInventorySelection(selected, self.blueprint))
                        return;
                    selectedFromInventory = self.blueprint;
                    infoPanel.ShowBlueprint(self.blueprint, null, true, true);
                }
                else
                {
                    selected = self.blueprint;
                    infoPanel.ShowBlueprint(self.blueprint, null, true, true);
                    selectedFromInventory = null;
                }
            }
            else
            {
                if (inventory)
                    selectedFromInventory = null;
                else
                    selected = null;
                infoPanel.Hide(true, true);
            }

            UpdateInterfaceState();
        }

        void UpdateInterfaceState()
        {
            if (selected == null)
            {
                instructions.text = "Select a blueprint to add";
                confirmButton.interactable = false;
                foreach (var display in offeredDisplays)
                    display.highlight.color = activeColor;
                foreach (var display in inventoryDisplays)
                    display.highlight.color = Color.clear;
            }
            else if (selectedFromInventory == null)
            {
                foreach (var display in offeredDisplays)
                    display.highlight.color = Color.clear;

                bool haveToExchange = true;
                string limitingFactorText = "";
                if (buildingLimited && selected.type != Blueprint.Blueprint.Type.Ability)
                    limitingFactorText = $"{maxBlueprintsOfOneKind} buildings";
                else if (abilityLimited && selected.type == Blueprint.Blueprint.Type.Ability)
                    limitingFactorText = $"{maxBlueprintsOfOneKind} abilities";
                else if (blueprintLimited)
                    limitingFactorText = $"{maxBlueprints} blueprints";
                else
                    haveToExchange = false;

                if (haveToExchange)
                {
                    instructions.text = $"Select a blueprint to exchange from your inventory\n(You can have at most {limitingFactorText})";
                    confirmButton.interactable = false;
                    foreach (var display in inventoryDisplays)
                        display.highlight.color = ValidInventorySelection(selected, display.blueprint) ? activeColor : Color.clear;
                }
                else
                {
                    instructions.text = "Add blueprint";
                    confirmButton.interactable = true;
                    foreach (var display in inventoryDisplays)
                        display.highlight.color = Color.clear;
                }
            }
            else
            {
                instructions.text = "Exchange blueprints";
                confirmButton.interactable = true;
                foreach (var display in inventoryDisplays)
                    display.highlight.color = Color.clear;
            }

            skipButton.interactable = selected == null && selectedFromInventory == null;

            foreach (var display in offeredDisplays)
            {
                display.selected = display.blueprint == selected;
                if (display.blueprint == selected)
                    display.highlight.color = selectedColor;
            }

            foreach (var display in inventoryDisplays)
            {
                display.selected = display.blueprint == selectedFromInventory;
                if (display.blueprint == selectedFromInventory)
                    display.highlight.color = selectedColor;
            }
        }

        bool ValidInventorySelection(Blueprint.Blueprint selected, Blueprint.Blueprint blueprint)
        {
            if (buildingLimited && selected.type != Blueprint.Blueprint.Type.Ability && blueprint.type != Blueprint.Blueprint.Type.Ability)
                return true;
            if (abilityLimited && selected.type == Blueprint.Blueprint.Type.Ability && blueprint.type == Blueprint.Blueprint.Type.Ability)
                return true;
            if (blueprintLimited)
                return true;
            return false;
        }
    }
}