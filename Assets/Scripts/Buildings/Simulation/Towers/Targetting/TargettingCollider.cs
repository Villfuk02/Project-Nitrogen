
using Attackers.Simulation;
using System.Linq;
using UnityEngine;

namespace Buildings.Simulation.Towers.Targetting
{
    internal class TargettingCollider : MonoBehaviour, ITargettingChild
    {
        ITargettingParent parent;
        public void InitParent(ITargettingParent parent)
        {
            this.parent = parent;
        }

        private void OnTriggerEnter(Collider other)
        {
            other.transform.parent.TryGetComponent(out Attacker attacker);
            if (attacker != null)
            {
                parent.TargetFound(attacker);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            other.transform.parent.TryGetComponent(out Attacker attacker);
            if (attacker != null)
            {
                parent.TargetLost(attacker);
            }
        }
        public bool IsInBounds(Vector3 pos)
        {
            Collider col = GetComponent<Collider>();
            return Physics.OverlapSphere(pos, 0.001f, 1 << gameObject.layer, QueryTriggerInteraction.Collide).Contains(col);
        }
    }
}

