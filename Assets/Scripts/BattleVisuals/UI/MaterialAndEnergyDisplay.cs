using BattleSimulation.Control;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] int animationDivisor;
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
            float energyFillAmount = Mathf.Clamp01(bc.Energy / (float)bc.MaxEnergy);
            energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, energyFillAmount, Time.deltaTime * 4);
            energyDisplay = bc.Energy - (bc.Energy - energyDisplay) * (animationDivisor - 1) / animationDivisor;
            energyText.text = $"{energyDisplay}<size=15>/{bc.MaxEnergy}</size>";

            materialDisplay = bc.Material - (bc.Material - materialDisplay) * (animationDivisor - 1) / animationDivisor;
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
