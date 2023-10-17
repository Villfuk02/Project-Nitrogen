using BattleSimulation.Control;
using TMPro;
using UnityEngine;

namespace BattleVisuals.UI
{
    public class MaterialDisplayPrototype : MonoBehaviour
    {
        [SerializeField] BattleController bc;
        [SerializeField] TextMeshProUGUI text;

        void Update()
        {
            text.text = $"{bc.Material} ({bc.Energy}/{bc.MaxEnergy}) F{bc.Fuel}";
        }
    }
}
