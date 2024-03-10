using BattleSimulation.Control;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace BattleVisuals.UI
{
    public class FuelDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject predictionArrowPrefab;
        [SerializeField] BattleController bc;
        [SerializeField] Image fill;
        [SerializeField] TextMeshProUGUI fuelText;
        [SerializeField] TextMeshProUGUI incomeText;
        [SerializeField] TextMeshProUGUI goalText;
        [SerializeField] RectTransform predictionArrowHolder;
        [Header("Settings")]
        [SerializeField] int convergenceDivisor;
        [SerializeField] float emptyWidth;
        [SerializeField] float minWidth;
        [SerializeField] float maxWidth;
        [SerializeField] Color incomeColor;
        [Header("Runtime variables")]
        [SerializeField] int fuelDisplay;
        [SerializeField] float currentWidth;
        [SerializeField] float incomeWidth;
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

            UpdateFill();
            UpdateFuelText();
            UpdateIncomeTextColor();
            UpdatePredictionArrows();
        }

        void UpdateFill()
        {
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
        }

        void UpdateFuelText()
        {
            MathUtils.StepTowards(ref fuelDisplay, bc.Fuel, convergenceDivisor);
            fuelText.text = fuelDisplay.ToString();
        }

        void UpdateIncomeTextColor()
        {
            var c = incomeColor;
            c.a = Mathf.Lerp(0, c.a, (maxWidth - currentWidth - incomeWidth) / incomeWidth);
            incomeText.color = c;
        }

        void UpdatePredictionArrows()
        {
            int remain;
            if (income_ <= 0)
                remain = -1;
            else
                remain = (bc.FuelGoal - bc.Fuel + income_ - 1) / income_;
            int arrows = Mathf.Max(remain - 1, 0);
            float targetSpacing = (maxWidth - minWidth) * income_ / bc.FuelGoal;
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

            float startPosition = Mathf.Max(currentWidth, minWidth);
            for (int i = 0; i < predictionArrows_.Count; i++)
            {
                predictionArrows_[i].anchoredPosition = Vector2.right * (startPosition + arrowSpacing_ * (i + 1));
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
