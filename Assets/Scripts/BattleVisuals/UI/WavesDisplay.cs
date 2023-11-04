using BattleSimulation.Control;
using TMPro;
using UnityEngine;

namespace BattleVisuals.UI
{
    public class WavesDisplay : MonoBehaviour
    {
        [SerializeField] WaveController wc;
        [SerializeField] TextMeshProUGUI waveNumber;
        [SerializeField] TextMeshProUGUI remainingText;

        void Update()
        {
            waveNumber.text = wc.wave.ToString();
            int remain = 10 - wc.wave;
            remainingText.text = remain switch
            {
                <= 0 => "<size=36>last\nwave</size>",
                1 => "<size=26>last\n\nwave left</size>",
                _ => $"{remain}\n<size=26>waves left</size>"
            };
        }
    }
}
