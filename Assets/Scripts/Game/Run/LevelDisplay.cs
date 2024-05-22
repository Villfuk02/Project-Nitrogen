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
        [Header("Runtime variables")]
        [SerializeField] int displayed;

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
        }
    }
}