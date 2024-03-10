using BattleSimulation.Control;
using Game.Run.Events;
using UnityEngine;
using Utils;

namespace BattleVisuals.UI
{
    public class OverlayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] RunEvents runEvents;
        [SerializeField] GameObject victoryOverlay;
        [SerializeField] GameObject defeatOverlay;

        void Awake()
        {
            Hide();
            BattleController.winLevel.RegisterReaction(OnVictory, 100);
            runEvents = GameObject.FindGameObjectWithTag(TagNames.RUN_PERSISTENCE).GetComponent<RunEvents>();
            runEvents.defeat.RegisterReaction(OnDefeat, 100);
        }

        void OnDestroy()
        {
            BattleController.winLevel.UnregisterReaction(OnVictory);
            runEvents.defeat.UnregisterReaction(OnDefeat);
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

        public void Restart()
        {
            runEvents.restart.Invoke();
        }

        public void Exit()
        {
            Application.Quit();
        }
    }
}
