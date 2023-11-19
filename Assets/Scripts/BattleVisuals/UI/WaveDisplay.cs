using BattleSimulation.Control;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BattleVisuals.UI
{
    public class WaveDisplay : MonoBehaviour
    {
        [SerializeField] GameObject waveBatchPrefab;
        [SerializeField] TextMeshProUGUI waveNumberText;
        [SerializeField] GameObject waveTag;
        readonly List<WaveBatchDisplay> batches_ = new();

        public void Init(WaveGenerator.Wave wave, int waveNumber, WavesDisplay wd)
        {
            waveNumberText.text = waveNumber.ToString();
            foreach (var batch in wave.batches)
            {
                var b = Instantiate(waveBatchPrefab, transform).GetComponent<WaveBatchDisplay>();
                batches_.Add(b);
                b.Init(batch, wd);
            }
        }

        public bool SpawnedOnce()
        {
            Destroy(waveTag);
            if (batches_[0].SpawnedOnce())
                batches_.RemoveAt(0);
            if (batches_.Count == 0)
            {
                Destroy(gameObject);
                return true;
            }
            return false;
        }
    }
}
