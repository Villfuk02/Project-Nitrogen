using BattleSimulation.Control;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.UI
{
    public class MaterialAndEnergyDisplay : MonoBehaviour
    {
        [SerializeField] BattleController bc;
        [SerializeField] TextMeshProUGUI energyText;
        [SerializeField] TextMeshProUGUI materialText;
        [SerializeField] TextMeshProUGUI energyIncomeText;
        [SerializeField] TextMeshProUGUI materialIncomeText;
        [SerializeField] Image energyFill;
        [SerializeField] int animationDivisor;
        [SerializeField] int energyDisplay;
        [SerializeField] int materialDisplay;

        void Update()
        {
            energyIncomeText.text = "+0";
            materialIncomeText.text = "+0";

            float energyFillAmount = Mathf.Clamp01(bc.Energy / (float)bc.MaxEnergy);
            energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, energyFillAmount, Time.deltaTime * 4);

            energyDisplay = bc.Energy - (bc.Energy - energyDisplay) * (animationDivisor - 1) / animationDivisor;
            materialDisplay = bc.Material - (bc.Material - materialDisplay) * (animationDivisor - 1) / animationDivisor;
            energyText.text = $"{energyDisplay}<size=15>/{bc.MaxEnergy}</size>";
            materialText.text = materialDisplay.ToString();
        }
    }
}
