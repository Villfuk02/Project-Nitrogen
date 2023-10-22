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
            text.text = $"{bc.Material}<sprite=0>  {bc.Energy}/{bc.MaxEnergy}<sprite=2>  {bc.Fuel}<sprite=1>";
        }
    }
}
