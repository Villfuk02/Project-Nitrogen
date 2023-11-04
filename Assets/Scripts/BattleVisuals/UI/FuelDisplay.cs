using BattleSimulation.Control;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.UI
{
    public class FuelDisplay : MonoBehaviour
    {
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
        void Update()
        {
            incomeText.text = "+0"; // TODO: hide once it's too close to the end 
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
        }
    }
}
