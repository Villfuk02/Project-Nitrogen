using System.Collections.Generic;
using BattleSimulation.Selection;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.Selection
{
    public class BlueprintMenuDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject blueprintMenuItemPrefab;
        [SerializeField] BlueprintMenu menu;
        [SerializeField] SelectionController selectionController;
        [SerializeField] RectTransform abilityHotbar;
        [SerializeField] RectTransform buildingHotbar;
        [SerializeField] LayoutElement hotbarContainer;
        [Header("Settings")]
        [SerializeField] float hiddenDisplacement;
        [SerializeField] float switchSpeed;
        [Header("Runtime variables")]
        [SerializeField] List<BlueprintMenuItem> menuItems;
        [SerializeField] float hotbarState;

        void Start()
        {
            InitItems();
            UpdateItems();
        }

        public void InitItems()
        {
            foreach (var item in menuItems)
                Destroy(item.gameObject);
            menuItems.Clear();

            foreach (var entry in menu.abilities)
                InitItem(entry, abilityHotbar);
            foreach (var entry in menu.buildings)
                InitItem(entry, buildingHotbar);
        }

        void InitItem(BlueprintMenu.MenuEntry entry, Transform parent)
        {
            var item = Instantiate(blueprintMenuItemPrefab, parent).GetComponent<BlueprintMenuItem>();
            item.Init(entry);
            item.display.onClick.AddListener(() => selectionController.SelectFromMenu(entry.index));
            item.display.onHover.AddListener(() => selectionController.HoverFromMenu(entry.index));
            item.display.onUnhover.AddListener(selectionController.UnhoverFromMenu);
            menuItems.Add(item);
        }

        void Update()
        {
            UpdateItems();
            UpdateHotbarPositions();
        }

        public void UpdateItems()
        {
            foreach (var item in menuItems)
                item.UpdateItem(menu.selected);
        }

        void UpdateHotbarPositions()
        {
            hotbarState = Mathf.Lerp(hotbarState, menu.waveStarted ? 1 : 0, Time.deltaTime * switchSpeed);
            abilityHotbar.anchoredPosition = hiddenDisplacement * (1 - hotbarState) * Vector2.down;
            buildingHotbar.anchoredPosition = hiddenDisplacement * hotbarState * Vector2.down;
            if (hotbarContainer.minWidth == 0)
            {
                hotbarContainer.minWidth = Mathf.Max(abilityHotbar.rect.width, buildingHotbar.rect.width);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)hotbarContainer.transform.parent);
            }
        }
    }
}