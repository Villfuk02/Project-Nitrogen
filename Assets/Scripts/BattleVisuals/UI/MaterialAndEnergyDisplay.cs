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

        void Awake()
        {
            BattleController.updateMaterialsPerWave.Register(UpdateMaterialsIncome, 0);
            BattleController.updateEnergyPerWave.Register(UpdateEnergyIncome, 0);
        }

        void OnDestroy()
        {
            BattleController.updateMaterialsPerWave.Unregister(UpdateMaterialsIncome);
            BattleController.updateEnergyPerWave.Unregister(UpdateEnergyIncome);
        }

        void Update()
        {
            float energyFillAmount = Mathf.Clamp01(bc.Energy / (float)bc.MaxEnergy);
            energyFill.fillAmount = Mathf.Lerp(energyFill.fillAmount, energyFillAmount, Time.deltaTime * 4);

            energyDisplay = bc.Energy - (bc.Energy - energyDisplay) * (animationDivisor - 1) / animationDivisor;
            materialDisplay = bc.Material - (bc.Material - materialDisplay) * (animationDivisor - 1) / animationDivisor;
            energyText.text = $"{energyDisplay}<size=15>/{bc.MaxEnergy}</size>";
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
