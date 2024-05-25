using Game.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Tutorial
{
    public class TutorialController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TutorialActions actions;
        [Header("Settings")]
        [SerializeField] UnityEvent[] eventsByPhase;
        [Header("Runtime variables")]
        [SerializeField] int phase;

        void Start()
        {
            phase = -1;
            if (!PersistentData.FinishedTutorial)
                TriggerPhase(0);
            else
                actions.enabled = false;
        }

        public void TriggerPhase(int phase)
        {
            if (phase != this.phase + 1)
                return;

            this.phase++;
            eventsByPhase[phase].Invoke();
        }
    }
}