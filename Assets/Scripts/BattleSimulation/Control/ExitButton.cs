using Game.Run.Events;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSimulation.Control
{
    public class ExitButton : MonoBehaviour
    {
        static readonly int Show = Animator.StringToHash("Show");
        static readonly int Hide = Animator.StringToHash("Hide");
        [Header("References")]
        [SerializeField] Button button;
        [SerializeField] Animator animator;
        [SerializeField] float exitDelay;
        [Header("Runtime variables")]
        [SerializeField] float activeTime;
        [SerializeField] bool active;

        public void Hover()
        {
            ShowPanel();
        }

        public void Unhover()
        {
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
                RunEvents.quit.Invoke();
        }

        void Update()
        {
            if (active)
                activeTime += Time.deltaTime;
            button.interactable = activeTime > exitDelay;
        }
    }
}