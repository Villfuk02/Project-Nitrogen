using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

namespace Game.Blueprint
{
    public class BlueprintDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] Image blueprintBackground;
        [SerializeField] Image cooldownOverlay;
        [SerializeField] TextMeshProUGUI costText;
        public Image icon;
        public Image highlight;

        [Header("Settings")]
        [SerializeField] Color defensiveBuildingColor;
        [SerializeField] Color economicBuildingColor;
        [SerializeField] Color abilityColor;
        [SerializeField] Color upgradeColor;
        [SerializeField] float hoveredIconScale;
        [SerializeField] float highlightPeriod;
        [SerializeField] float highlightScale;
        [SerializeField] float selectedHighlightScale;
        [SerializeField] float largeFont;
        [SerializeField] float smallFont;

        [Header("Runtime variables")]
        public Blueprint blueprint;
        [SerializeField] bool hovered;
        public bool selected;
        public float targetCooldownFill;
        float timer_;
        public UnityEvent onClick;

        public void InitBlueprint(Blueprint b)
        {
            blueprint = b;
            blueprintBackground.color = GetColorFromType(blueprint.type);
            icon.sprite = blueprint.icon;
        }

        void Update()
        {
            icon.transform.localScale = Mathf.Lerp(icon.transform.localScale.x, hovered || selected ? hoveredIconScale : 1, Time.deltaTime * 20) * Vector3.one;
            cooldownOverlay.fillAmount = Mathf.Lerp(cooldownOverlay.fillAmount, targetCooldownFill, Time.deltaTime * 5);
            timer_ += Time.deltaTime;
            float targetHighlightScale;
            if (hovered || selected)
                targetHighlightScale = selectedHighlightScale;
            else
                targetHighlightScale = highlightScale + Mathf.Sin(timer_ * 2 * MathF.PI / highlightPeriod) * (highlightScale - 1) * 0.5f;
            highlight.transform.localScale = Mathf.Lerp(highlight.transform.localScale.x, targetHighlightScale, Time.deltaTime * 40) * Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            onClick.Invoke();
        }

        public void UpdateText(int energy, int materials, Color color)
        {
            string text = GetTextFromCost(energy, materials, out var useSmallerFont);
            costText.text = text;
            costText.fontSize = useSmallerFont ? smallFont : largeFont;
            costText.color = color;
        }

        Color GetColorFromType(Blueprint.Type type) => type switch
        {
            Blueprint.Type.DefensiveBuilding => defensiveBuildingColor,
            Blueprint.Type.EconomicBuilding => economicBuildingColor,
            Blueprint.Type.Ability => abilityColor,
            Blueprint.Type.Upgrade => upgradeColor,
            Blueprint.Type.SpecialBuilding => economicBuildingColor,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        static string GetTextFromCost(int energy, int materials, out bool useSmallerFont)
        {
            useSmallerFont = false;
            if (energy <= 0 && materials <= 0)
                return "FREE";

            string res = "";
            int parts = 0;
            if (energy > 0)
            {
                res += $"{energy}{TextUtils.Icon.Energy.Sprite()}";
                parts++;
            }
            if (materials > 0)
            {
                res += $"{materials}{TextUtils.Icon.Materials.Sprite()}";
                parts++;
            }

            useSmallerFont = parts > 1;
            return res;
        }
    }
}
