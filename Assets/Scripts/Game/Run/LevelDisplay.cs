using TMPro;
using UnityEngine;

namespace Game.Run
{
    public class LevelDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] RunPersistence rp;
        [SerializeField] TextMeshProUGUI txt;
        [Header("Runtime variables")]
        [SerializeField] int displayed;

        void Update()
        {
            if (rp.level == displayed)
                return;

            displayed = rp.level;
            txt.text = $"Level {displayed}";
        }
    }
}
