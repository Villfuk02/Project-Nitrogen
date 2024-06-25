using BattleSimulation.Control;
using Game.Run.Events;
using UnityEngine;

namespace BattleVisuals.UI
{
    public class OverlayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject victoryOverlay;
        [SerializeField] GameObject defeatOverlay;

        void Awake()
        {
            BattleController.WIN_LEVEL.RegisterReaction(OnVictory, 100);
            RunEvents.defeat.RegisterReaction(OnDefeat, 100);

            Hide();
        }

        void OnDestroy()
        {
            BattleController.WIN_LEVEL.UnregisterReaction(OnVictory);
            RunEvents.defeat.UnregisterReaction(OnDefeat);
        }

        void OnVictory()
        {
            victoryOverlay.SetActive(true);
        }

        void OnDefeat()
        {
            defeatOverlay.SetActive(true);
        }

        public void Hide()
        {
            victoryOverlay.SetActive(false);
            defeatOverlay.SetActive(false);
        }

        public void Quit()
        {
            RunEvents.quit.Invoke();
        }
    }
}