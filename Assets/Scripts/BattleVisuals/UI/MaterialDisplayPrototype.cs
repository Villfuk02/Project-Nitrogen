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
            text.text = $"{bc.material} ({bc.energy}/{bc.maxEnergy})";
        }
    }
}
