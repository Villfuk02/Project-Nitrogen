using Game.Run.Shared;
using UnityEngine;

namespace BattleSimulation.Control
{
    public class ExitButton : MonoBehaviour
    {
        static readonly int Show = Animator.StringToHash("Show");
        static readonly int Hide = Animator.StringToHash("Hide");
        [Header("References")]
        [SerializeField] Animator animator;

        public void ShowPanel()
        {
            animator.SetTrigger(Show);
        }

        public void HidePanel()
        {
            animator.SetTrigger(Hide);
        }

        public void Exit()
        {
            RunEvents.quit.Invoke();
        }
    }
}