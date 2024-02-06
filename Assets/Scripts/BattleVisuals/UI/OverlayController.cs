using BattleSimulation.Control;
using Game.Run.Events;
using UnityEngine;

namespace BattleVisuals.UI
{
    public class OverlayController : MonoBehaviour
    {
        [SerializeField] RunEvents runEvents;
        [SerializeField] GameObject victoryOverlay;
        [SerializeField] GameObject defeatOverlay;

        void Awake()
        {
            Hide();
            BattleController.winLevel.Register(OnVictory, 100);
            runEvents = GameObject.FindGameObjectWithTag("RunPersistence").GetComponent<RunEvents>();
            runEvents.defeat.Register(OnDefeat, 100);
        }

        void OnDestroy()
        {
            BattleController.winLevel.Unregister(OnVictory);
            runEvents.defeat.Unregister(OnDefeat);
        }

        bool OnVictory()
        {
            victoryOverlay.SetActive(true);
            return true;
        }

        bool OnDefeat()
        {
            defeatOverlay.SetActive(true);
            return true;
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
