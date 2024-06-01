using BattleSimulation.World.WorldData;
using TMPro;
using UnityEngine;

namespace Game.Run
{
    public class LevelDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] RunPersistence rp;
        [SerializeField] TextMeshProUGUI txt;
        [SerializeField] TextMeshProUGUI seedText;
        [SerializeField] Canvas canvas;
        [Header("Settings")]
        [SerializeField] int normalLayer;
        [SerializeField] int generatingLayer;
        [Header("Runtime variables")]
        [SerializeField] int displayed;
        [SerializeField] bool generating;

        void Start()
        {
            displayed = -1;
        }

        void Update()
        {
            if (string.IsNullOrEmpty(seedText.text))
            {
                seedText.text = $"Seed: {rp.seedString}";
            }

            if (rp.level != displayed)
            {
                displayed = rp.level;
                txt.text = $"Level {displayed}";
            }

            if (generating && World.instance != null && World.instance.Ready)
            {
                generating = false;
                Invoke(nameof(FinishedGenerating), 0.3f);
            }
        }

        public void StartedGenerating()
        {
            canvas.sortingOrder = generatingLayer;
            generating = true;
        }


        public void FinishedGenerating()
        {
            canvas.sortingOrder = normalLayer;
        }
    }
}