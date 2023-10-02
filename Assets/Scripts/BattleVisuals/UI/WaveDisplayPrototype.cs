using BattleSimulation.Control;
using TMPro;
using UnityEngine;

namespace BattleVisuals.UI
{
    public class WaveDisplayPrototype : MonoBehaviour
    {
        [SerializeField] WaveController wc;
        [SerializeField] TextMeshProUGUI text;

        void Update()
        {
            text.text = $"Wave {wc.wave}: {wc.attackersLeft + wc.toSpawn} left";
        }
    }
}
