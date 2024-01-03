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

        void Start()
        {
            Hide();
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

        public void ShowBlueprint(Blueprint.Blueprint blueprint)
        {
            Show();
            title.text = blueprint.name;
            icon.sprite = blueprint.icon;
            description.text = new DescriptionFormatter(blueprint).Format(DescriptionGenerator.GenerateBlueprintDescription(blueprint));
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }
    }
}
