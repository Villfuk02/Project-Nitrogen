using BattleSimulation.Attackers;
using BattleSimulation.Buildings;
using BattleSimulation.Targeting;
using BattleSimulation.Towers;
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
        [SerializeField] RectTransform targetingSection;
        [SerializeField] TextMeshProUGUI targetingText;
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
            UpdateTargeting();
        }

        void UpdateDescription(string desc)
        {
            deleteButton.gameObject.SetActive(building_ != null && !building_.permanent);
            description.text = desc;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        void UpdateTargeting()
        {
            if (building_ is Tower t)
            {
                Targeting targeting = t.targeting;
                targetingSection.gameObject.SetActive(targeting.CanChangePriority);
                targetingText.text = t.targeting.CurrentPriority;
            }
            else
            {
                targetingSection.gameObject.SetActive(false);
            }
        }

        public void NextPriority()
        {
            if (building_ is Tower t)
                t.targeting.NextPriority();
            UpdateTargeting();
        }

        public void PrevPriority()
        {
            if (building_ is Tower t)
                t.targeting.PrevPriority();
            UpdateTargeting();
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
            building_ = null;
            title.text = blueprint.name;
            icon.sprite = blueprint.icon;
            descriptionProvider_ = new BlueprintDescriptionProvider(blueprint, original);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
            Show();
        }

        public void ShowAttacker(Attacker attacker)
        {
            building_ = null;
            title.text = attacker.stats.name;
            icon.sprite = attacker.stats.icon;
            descriptionProvider_ = new AttackerDescriptionProvider(attacker);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
            Show();
        }
        public void ShowAttacker(AttackerStats.AttackerStats stats, AttackerStats.AttackerStats original)
        {
            building_ = null;
            title.text = stats.name;
            icon.sprite = stats.icon;
            descriptionProvider_ = new AttackerDescriptionProvider(stats, original);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
            Show();
        }

        public void ShowTile(Tile tile)
        {
            building_ = null;
            title.text = "Tile";
            icon.sprite = tileIcon;
            descriptionProvider_ = new TileDescriptionProvider(tile);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
            Show();
        }

        public void ShowBuilding(Building building)
        {
            building_ = building;
            title.text = building.Blueprint.name;
            icon.sprite = building.Blueprint.icon;
            descriptionProvider_ = new BlueprintDescriptionProvider(building);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
            Show();
        }
    }
}
