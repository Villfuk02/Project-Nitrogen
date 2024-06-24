using BattleSimulation.Control;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace BattleVisuals.UI
{
    public class MaterialAndEnergyDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BattleController bc;
        [SerializeField] TextMeshProUGUI energyText;
        [SerializeField] TextMeshProUGUI materialText;
        [SerializeField] TextMeshProUGUI energyIncomeText;
        [SerializeField] TextMeshProUGUI materialIncomeText;
        [SerializeField] Image energyFill;
        [Header("Settings")]
        [SerializeField] int convergenceDivisor;
        [Header("Runtime variables")]
        [SerializeField] int energyDisplay;
        [SerializeField] int materialDisplay;

        void Update()
        {
            float energyFillAmount = Mathf.Clamp01(bc.energy / (float)bc.maxEnergy);
            energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, energyFillAmount, Time.deltaTime * 4);
            MathUtils.StepTowards(ref energyDisplay, bc.energy, convergenceDivisor);
            energyText.text = $"{energyDisplay}<size=15>/{bc.maxEnergy}</size>";
            int energyIncome = BattleController.ENERGY_PER_WAVE.Query(new());
            energyIncomeText.text = energyIncome == 0 ? "" : $"+{Mathf.RoundToInt(energyIncome)}";

            MathUtils.StepTowards(ref materialDisplay, bc.material, convergenceDivisor);
            materialText.text = materialDisplay.ToString();
            int materialIncome = BattleController.MATERIALS_PER_WAVE.Query(new());
            materialIncomeText.text = materialIncome == 0 ? "" : $"+{Mathf.RoundToInt(materialIncome)}";
        }
    }
}