using BattleSimulation.Selection;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class BlueprintMenuDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject blueprintMenuItemPrefab;
        [SerializeField] BlueprintMenu menu;
        [SerializeField] SelectionController selectionController;
        [SerializeField] Transform hotbar;
        [Header("Runtime variables")]
        [SerializeField] BlueprintMenuItem[] menuItems;

        void Start()
        {
            InitItems();
            UpdateItems();
        }

        void InitItems()
        {
            menuItems = new BlueprintMenuItem[menu.blueprints.Length];
            for (int i = 0; i < menuItems.Length; i++)
            {
                menuItems[i] = Instantiate(blueprintMenuItemPrefab, hotbar).GetComponent<BlueprintMenuItem>();
                menuItems[i].display.InitBlueprint(menu.blueprints[i]);
                int index = i;
                menuItems[i].display.onClick.AddListener(() => selectionController.SelectFromMenu(index));
            }
        }

        void Update()
        {
            UpdateItems();
        }

        public void UpdateItems()
        {
            for (int i = 0; i < menuItems.Length; i++)
            {
                menuItems[i].UpdateItem(menu.cooldowns[i], menu.waveStarted, menu.selected == i);
            }
        }
    }
}
