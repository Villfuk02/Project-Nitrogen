using Game.Blueprint;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Selection
{
    public abstract class Placement : MonoBehaviour
    {
        [Header("References")]
        public Blueprinted blueprinted;
        [Header("Settings")]
        public UnityEvent onPlaced;
        public bool selectAttackers;

        public abstract bool Setup(Selectable? selected, int rotation, Vector3? pos, Transform defaultParent);
        public abstract bool IsValid();
        public abstract bool IsCorrectTypeSelected();

        public virtual void Place()
        {
            blueprinted.Place();
            onPlaced.Invoke();
        }
    }
}