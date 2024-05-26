using TMPro;
using UnityEngine;

namespace Game.Shared
{
    public class VersionDisplay : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI versionText;

        void Awake()
        {
            versionText.text = $"v{Application.version}";
        }
    }
}