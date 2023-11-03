using BattleSimulation.Selection;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class BlueprintMenuDisplay : MonoBehaviour
    {
        [SerializeField] GameObject blueprintMenuItemPrefab;
        [SerializeField] BlueprintMenu menu;
        [SerializeField] Transform hotbar;
        [SerializeField] BlueprintMenuItem[] menuItems;

        void Start()
        {
            menuItems = new BlueprintMenuItem[menu.blueprints.Length];
            for (int i = 0; i < menuItems.Length; i++)
            {
                menuItems[i] = Instantiate(blueprintMenuItemPrefab, hotbar).GetComponent<BlueprintMenuItem>();
                menuItems[i].display.InitBlueprint(menu.blueprints[i]);
            }
            UpdateItems();
        }

        void Update()
        {
            UpdateItems();
        }

        public void UpdateItems()
        {
            for (int i = 0; i < menuItems.Length; i++)
            {
                menuItems[i].UpdateItem(menu.cooldowns[i], menu.selected == i);
            }
        }
    }
}
