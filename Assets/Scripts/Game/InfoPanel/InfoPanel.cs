using BattleSimulation.Attackers;
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
        DescriptionProvider? descriptionProvider_;

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
            description.text = desc;
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        public void ShowBlueprint(Blueprint.Blueprint blueprint, Blueprint.Blueprint original)
        {
            Show();
            title.text = blueprint.name;
            icon.sprite = blueprint.icon;
            descriptionProvider_ = new BlueprintDescriptionProvider(blueprint, original);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
        }

        public void ShowAttacker(Attacker attacker)
        {
            Show();
            title.text = attacker.stats.name;
            icon.sprite = attacker.stats.icon;
            descriptionProvider_ = new AttackerDescriptionProvider(attacker);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
        }
        public void ShowAttacker(AttackerStats.AttackerStats stats, AttackerStats.AttackerStats original)
        {
            Show();
            title.text = stats.name;
            icon.sprite = stats.icon;
            descriptionProvider_ = new AttackerDescriptionProvider(stats, original);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
        }

        public void ShowTile(Tile tile)
        {
            Show();
            title.text = "Tile";
            icon.sprite = tileIcon;
            descriptionProvider_ = new TileDescriptionProvider(tile);
            descriptionProvider_.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
        }
    }
}
