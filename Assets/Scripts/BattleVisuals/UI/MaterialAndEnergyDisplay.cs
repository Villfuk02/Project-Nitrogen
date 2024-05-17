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

        void Awake()
        {
            BattleController.updateMaterialsPerWave.RegisterHandler(UpdateMaterialsIncome);
            BattleController.updateEnergyPerWave.RegisterHandler(UpdateEnergyIncome);
        }

        void OnDestroy()
        {
            BattleController.updateMaterialsPerWave.UnregisterHandler(UpdateMaterialsIncome);
            BattleController.updateEnergyPerWave.UnregisterHandler(UpdateEnergyIncome);
        }

        void Update()
        {
            float energyFillAmount = Mathf.Clamp01(bc.energy / (float)bc.maxEnergy);
            energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, energyFillAmount, Time.deltaTime * 4);
            MathUtils.StepTowards(ref energyDisplay, bc.energy, convergenceDivisor);
            energyText.text = $"{energyDisplay}<size=15>/{bc.maxEnergy}</size>";

            MathUtils.StepTowards(ref materialDisplay, bc.material, convergenceDivisor);
            materialText.text = materialDisplay.ToString();
        }

        bool UpdateMaterialsIncome(ref float income)
        {
            materialIncomeText.text = $"+{Mathf.RoundToInt(income)}";
            return true;
        }

        bool UpdateEnergyIncome(ref float income)
        {
            energyIncomeText.text = $"+{Mathf.RoundToInt(income)}";
            return true;
        }
    }
}