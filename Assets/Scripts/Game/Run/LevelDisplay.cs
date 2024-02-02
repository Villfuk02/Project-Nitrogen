using TMPro;
using UnityEngine;

namespace Game.Run
{
    public class LevelDisplay : MonoBehaviour
    {
        [SerializeField] RunPersistence rp;
        [SerializeField] TextMeshProUGUI txt;
        int displayed_;

        void Update()
        {
            if (rp.level != displayed_)
            {
                displayed_ = rp.level;
                txt.text = $"Level {displayed_}";
            }
        }
    }
}
