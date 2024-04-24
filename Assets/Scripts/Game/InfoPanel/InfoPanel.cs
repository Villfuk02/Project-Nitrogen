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
        [Header("References")]
        [SerializeField] RectTransform root;
        [SerializeField] Image backgroundPanel;
        [SerializeField] TextMeshProUGUI title;
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI description;
        [SerializeField] RectTransform deleteButton;
        [SerializeField] RectTransform targetingSection;
        [SerializeField] TextMeshProUGUI targetingText;
        [SerializeField] Graphic[] raycastTargets;
        [Header("Settings")]
        [SerializeField] Sprite tileIcon;
        [SerializeField] Color lowPriorityBackgroundColor;
        [Header("Runtime variables")]
        [SerializeField] bool visible;
        [SerializeField] bool allowInteraction;
        [SerializeField] bool priority;
        [SerializeField] bool rebuildLayout;
        Data? current_;
        Data? fallback_;
        class Data
        {
            public string title;
            public Sprite sprite;
            public DescriptionProvider descriptionProvider;
            public Building building;
        }

        void Start()
        {
            Hide();
        }

        void Update()
        {
            if (rebuildLayout)
            {
                rebuildLayout = false;
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            }
            if (!visible || current_?.descriptionProvider == null || !current_.descriptionProvider.HasDescriptionChanged(out var desc))
                return;
            UpdateDescription(desc);
        }

        public void Hide(bool highPriority, bool clearFallback)
        {
            if (priority && !highPriority)
                return;
            if (clearFallback)
                fallback_ = null;
            if (fallback_ != null)
                DisplayData(fallback_, highPriority, true);
            else
                Hide();
        }

        void Hide()
        {
            priority = false;
            current_ = null;
            if (!visible)
                return;
            visible = false;
            UpdateDescription("");
            root.gameObject.SetActive(false);
        }

        void DisplayData(Data data, bool highPriority, bool fallback)
        {
            if (priority && !highPriority)
                return;

            if (fallback)
                fallback_ = data;
            allowInteraction = fallback;
            priority = highPriority;

            if (data == null)
            {
                Hide();
                return;
            }
            current_ = data;
            title.text = data.title;
            icon.sprite = data.sprite;
            current_.descriptionProvider.HasDescriptionChanged(out var desc);
            UpdateDescription(desc);
            Show();
        }

        void Show()
        {
            if (visible)
                return;
            visible = true;
            root.gameObject.SetActive(true);
            foreach (var target in raycastTargets)
                target.raycastTarget = priority;
            backgroundPanel.color = priority ? Color.white : lowPriorityBackgroundColor;
            UpdateTargeting();
        }

        void UpdateDescription(string desc)
        {
            deleteButton.gameObject.SetActive(current_?.building != null && !current_.building.permanent && allowInteraction);
            UpdateTargeting();
            description.text = desc;
            rebuildLayout = true;
        }

        void UpdateTargeting()
        {
            if (current_ != null && current_.building != null && current_.building is Tower t)
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
            if (!allowInteraction || current_?.building is not Tower t)
                return;

            t.targeting.NextPriority();
            UpdateTargeting();
        }

        public void PrevPriority()
        {
            if (!allowInteraction || current_?.building is not Tower t)
                return;

            t.targeting.PrevPriority();
            UpdateTargeting();
        }

        public void DeleteBuilding()
        {
            if (!allowInteraction || current_?.building == null || current_.building.permanent)
                return;

            current_.building!.Delete();
            if (visible && current_.descriptionProvider != null)
            {
                current_.descriptionProvider.HasDescriptionChanged(out string desc);
                UpdateDescription(desc);
            }
        }

        public void ShowBlueprint(Blueprint.Blueprint blueprint, Blueprint.Blueprint original, bool highPriority, bool fallback)
        {
            Data d = new()
            {
                building = null,
                descriptionProvider = new BlueprintDescriptionProvider(blueprint, original),
                sprite = blueprint.icon,
                title = blueprint.name
            };
            DisplayData(d, highPriority, fallback);
        }

        public void ShowAttacker(Attacker attacker, bool highPriority, bool fallback)
        {
            Data d = new()
            {
                building = null,
                descriptionProvider = new AttackerDescriptionProvider(attacker),
                sprite = attacker.stats.icon,
                title = attacker.stats.name
            };
            DisplayData(d, highPriority, fallback);
        }

        public void ShowAttacker(AttackerStats.AttackerStats stats, AttackerStats.AttackerStats original, bool highPriority, bool fallback)
        {
            Data d = new()
            {
                building = null,
                descriptionProvider = new AttackerDescriptionProvider(stats, original),
                title = stats.name,
                sprite = stats.icon
            };
            DisplayData(d, highPriority, fallback);
        }

        public void ShowTile(Tile tile, bool highPriority, bool fallback)
        {
            Data d = new()
            {
                building = null,
                descriptionProvider = new TileDescriptionProvider(tile),
                title = "Tile",
                sprite = tileIcon
            };
            DisplayData(d, highPriority, fallback);
        }

        public void ShowBuilding(Building building, bool highPriority, bool fallback)
        {
            Data d = new()
            {
                building = building,
                descriptionProvider = new BlueprintDescriptionProvider(building),
                title = building.Blueprint.name,
                sprite = building.Blueprint.icon
            };
            DisplayData(d, highPriority, fallback);
        }
    }
}
