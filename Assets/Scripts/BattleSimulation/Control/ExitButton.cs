using Game.Shared;
using UnityEngine;

namespace BattleSimulation.Control
{
    public class ExitButton : MonoBehaviour
    {
        static readonly int Show = Animator.StringToHash("Show");
        static readonly int Hide = Animator.StringToHash("Hide");
        [Header("References")]
        [SerializeField] Animator animator;
        [SerializeField] float exitDelay;
        [SerializeField] float hideDelay;
        [Header("Runtime variables")]
        [SerializeField] float activeTime;
        [SerializeField] bool active;
        [SerializeField] bool hover;

        public void Hover()
        {
            hover = true;
            ShowPanel();
        }

        public void Unhover()
        {
            hover = false;
            HidePanel();
        }

        void ShowPanel()
        {
            if (active)
                return;
            active = true;
            animator.SetTrigger(Show);
        }

        void HidePanel()
        {
            if (!active)
                return;
            active = false;
            activeTime = 0;
            animator.SetTrigger(Hide);
        }

        public void TryExit()
        {
            if (activeTime > exitDelay)
            {
                RunEvents.quit.Invoke();
            }
            else
            {
                ButtonSounds.Hover();
                ShowPanel();
            }
        }

        void Update()
        {
            if (active)
                activeTime += Time.deltaTime;
            if (activeTime > hideDelay && !hover)
                HidePanel();
            /*
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                TryExit();
                ButtonSounds.Click();
            }
            */
        }
    }
}