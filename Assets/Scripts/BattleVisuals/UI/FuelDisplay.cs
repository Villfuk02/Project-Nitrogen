using BattleSimulation.Control;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.UI
{
    public class FuelDisplay : MonoBehaviour
    {
        [SerializeField] GameObject predictionArrowPrefab;
        [SerializeField] BattleController bc;
        [SerializeField] Image fill;
        [SerializeField] TextMeshProUGUI fuelText;
        [SerializeField] TextMeshProUGUI incomeText;
        [SerializeField] TextMeshProUGUI goalText;
        [SerializeField] int animationDivisor;
        [SerializeField] int fuelDisplay;
        [SerializeField] float emptyWidth;
        [SerializeField] float minWidth;
        [SerializeField] float maxWidth;
        [SerializeField] float currentWidth;
        [SerializeField] float incomeWidth;
        [SerializeField] Color incomeColor;
        [SerializeField] RectTransform predictionArrowHolder;
        readonly List<RectTransform> predictionArrows_ = new();
        int income_;
        float arrowSpacing_;

        void Awake()
        {
            BattleController.updateFuelPerWave.RegisterHandler(UpdateFuelIncome);
        }

        void OnDestroy()
        {
            BattleController.updateFuelPerWave.UnregisterHandler(UpdateFuelIncome);
        }

        void Update()
        {
            goalText.text = bc.FuelGoal.ToString();

            float targetWidth;
            if (bc.Fuel == 0)
            {
                fill.color = Color.clear;
                targetWidth = emptyWidth;
            }
            else
            {
                fill.color = Color.white;
                targetWidth = Mathf.Lerp(minWidth, maxWidth, bc.Fuel / (float)bc.FuelGoal);
            }

            currentWidth = Mathf.Lerp(currentWidth, targetWidth, 10 * Time.deltaTime);
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentWidth);

            fuelDisplay = bc.Fuel - (bc.Fuel - fuelDisplay) * (animationDivisor - 1) / animationDivisor;
            fuelText.text = fuelDisplay.ToString();

            var c = incomeColor;
            c.a = Mathf.Lerp(0, c.a, (maxWidth - currentWidth - incomeWidth) / incomeWidth);
            incomeText.color = c;

            int remain;
            if (income_ <= 0)
                remain = -1;
            else
                remain = (bc.FuelGoal - bc.Fuel + income_ - 1) / income_;
            int arrows = Mathf.Max(remain - 1, 0);
            float targetSpacing = (maxWidth - minWidth) * income_ / bc.FuelGoal;
            if (bc.Fuel <= 0)
            {
                targetSpacing = 0;
                arrows = 0;
            }
            arrowSpacing_ = Mathf.Lerp(arrowSpacing_, targetSpacing, Time.deltaTime * 10);
            if (predictionArrows_.Count < arrows)
            {
                predictionArrows_.Add(Instantiate(predictionArrowPrefab, predictionArrowHolder).GetComponent<RectTransform>());
            }
            else if (predictionArrows_.Count > arrows)
            {
                Destroy(predictionArrows_[^1].gameObject);
                predictionArrows_.RemoveAt(predictionArrows_.Count - 1);
            }

            for (int i = 0; i < predictionArrows_.Count; i++)
            {
                predictionArrows_[i].anchoredPosition = Vector2.right * (arrowSpacing_ * (i + 1));
            }
        }

        bool UpdateFuelIncome(ref float income)
        {
            income = Mathf.FloorToInt(income);
            income_ = (int)income;
            incomeText.text = $"+{income:F0}";
            return true;
        }
    }
}
