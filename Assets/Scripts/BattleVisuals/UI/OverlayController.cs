using BattleSimulation.Control;
using UnityEngine;

namespace BattleVisuals.UI
{
    public class OverlayController : MonoBehaviour
    {
        [SerializeField] GameObject victoryOverlay;

        void Awake()
        {
            victoryOverlay.SetActive(false);
            BattleController.winLevel.Register(OnVictory, 100);
        }

        void OnDestroy()
        {
            BattleController.winLevel.Unregister(OnVictory);
        }

        bool OnVictory()
        {
            victoryOverlay.SetActive(true);
            return true;
        }

        public void Hide()
        {
            victoryOverlay.SetActive(false);
        }
    }
}
