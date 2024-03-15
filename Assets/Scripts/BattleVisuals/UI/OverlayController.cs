using BattleSimulation.Control;
using Game.Run.Events;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace BattleVisuals.UI
{
    public class OverlayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] RunEvents runEvents;
        [SerializeField] GameObject victoryOverlay;
        [SerializeField] GameObject defeatOverlay;
        [SerializeField] Image fade;
        [SerializeField] TextMeshProUGUI loadingText;

        void Awake()
        {
            BattleController.winLevel.RegisterReaction(OnVictory, 100);
            runEvents = GameObject.FindGameObjectWithTag(TagNames.RUN_PERSISTENCE).GetComponent<RunEvents>();
            runEvents.defeat.RegisterReaction(OnDefeat, 100);

            Hide();
            fade.enabled = true;
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

        public void FadeIn(float time)
        {
            StartCoroutine(Fade(-1 / time));
        }

        public void FadeOut(float time)
        {
            StartCoroutine(Fade(1 / time));
        }

        IEnumerator Fade(float speed)
        {
            fade.enabled = true;
            float alpha = fade.color.a;
            do
            {
                alpha += Time.deltaTime * speed;
                fade.color = new(0, 0, 0, alpha);
                loadingText.color = new(1, 1, 1, 2 * alpha - 1);
                yield return null;
            } while (alpha is > 0 and < 1);

            if (alpha <= 0)
                fade.enabled = false;
        }

    }
}
