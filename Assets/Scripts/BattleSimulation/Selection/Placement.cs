using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Selection
{
    public abstract class Placement : MonoBehaviour
    {
        public UnityEvent onPlaced;

        public abstract bool Setup(Selectable selected, int rotation, Vector3 pos, Transform defaultParent);
        public abstract bool IsValid();

        public virtual void Place()
        {
            onPlaced.Invoke();
        }
    }
}
