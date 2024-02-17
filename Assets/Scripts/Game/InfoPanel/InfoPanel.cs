using BattleSimulation.Attackers;
using BattleSimulation.Buildings;
using BattleSimulation.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.InfoPanel
{
    public class InfoPanel : MonoBehaviour
    {
        [SerializeField] RectTransform root;
        [SerializeField] TextMeshProUGUI title;
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI description;
        [SerializeField] bool visible;
        [SerializeField] Sprite tileIcon;
        [SerializeField] RectTransform deleteButton;
        DescriptionProvider? descriptionProvider_;
        Building? building_;

        void Start()
        {
            Hide();
        }

        void Update()
        {
            if (!visible || descriptionProvider_ == null || !descriptionProvider_.HasDescriptionChanged(out var desc))
                return;
            UpdateDescription(desc);
        }

        public void Hide()
        {
            if (!visible)
                return;
            visible = false;
            root.gameObject.SetActive(false);
        }

        void Show()
        {
            if (visible)
                return;
            visible = true;
            root.gameObject.SetActive(true);
        }

        void UpdateDescription(string desc)
        {
            deleteButton.gameObject.SetActive(building_ != null && !building_.permanent);
            description.text = desc;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        public void DeleteBuilding()
        {
            if (building_ == null || building_.permanent)
                return;

            building_!.Delete();
            if (visible && descriptionProvider_ != null)
            {
                descriptionProvider_.HasDescriptionChanged(out string desc);
                UpdateDescription(desc);
            }
        }

        public void ShowBlueprint(Blueprint.Blueprint blueprint, Blueprint.Blueprint original)
        {
            Show();
            building_ = null;
            title.text = blueprint.name;
            icon.sprite = blueprint.icon;
            descriptionProvider_ = new BlueprintDescriptionProvider(blueprint, original);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
        }

        public void ShowAttacker(Attacker attacker)
        {
            Show();
            building_ = null;
            title.text = attacker.stats.name;
            icon.sprite = attacker.stats.icon;
            descriptionProvider_ = new AttackerDescriptionProvider(attacker);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
        }
        public void ShowAttacker(AttackerStats.AttackerStats stats, AttackerStats.AttackerStats original)
        {
            Show();
            building_ = null;
            title.text = stats.name;
            icon.sprite = stats.icon;
            descriptionProvider_ = new AttackerDescriptionProvider(stats, original);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
        }

        public void ShowTile(Tile tile)
        {
            Show();
            building_ = null;
            title.text = "Tile";
            icon.sprite = tileIcon;
            descriptionProvider_ = new TileDescriptionProvider(tile);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
        }

        public void ShowBuilding(Building building)
        {
            Show();
            building_ = building;
            title.text = building.Blueprint.name;
            icon.sprite = building.Blueprint.icon;
            descriptionProvider_ = new BlueprintDescriptionProvider(building);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
        }
    }
}
