using BattleSimulation.Control;
using Game.Run;
using UnityEngine;

namespace BattleVisuals.UI
{
    public class OverlayController : MonoBehaviour
    {
        [SerializeField] RunPersistence rp;
        [SerializeField] GameObject victoryOverlay;
        [SerializeField] GameObject defeatOverlay;

        void Awake()
        {
            Hide();
            BattleController.winLevel.Register(OnVictory, 100);
            rp = GameObject.FindGameObjectWithTag("RunPersistence").GetComponent<RunPersistence>();
            rp.defeat.Register(OnDefeat, 100);
        }

        void OnDestroy()
        {
            BattleController.winLevel.Unregister(OnVictory);
            rp.defeat.Unregister(OnDefeat);
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
            rp.Restart();
        }

        public void Exit()
        {
            Application.Quit();
        }
    }
}
