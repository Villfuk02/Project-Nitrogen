
using Attackers.Simulation;
using System.Linq;
using UnityEngine;

namespace Buildings.Simulation.Towers.Targeting
{
    internal class TargetingCollider : MonoBehaviour, ITargetingChild
    {
        ITargetingParent parent_;
        public void InitParent(ITargetingParent targetingParent)
        {
            parent_ = targetingParent;
        }

        void OnTriggerEnter(Collider other)
        {
            other.transform.parent.TryGetComponent(out Attacker attacker);
            if (attacker != null)
            {
                parent_.TargetFound(attacker);
            }
        }

        void OnTriggerExit(Collider other)
        {
            other.transform.parent.TryGetComponent(out Attacker attacker);
            if (attacker != null)
            {
                parent_.TargetLost(attacker);
            }
        }
        public bool IsInBounds(Vector3 pos)
        {
            Collider col = GetComponent<Collider>();
            return Physics.OverlapSphere(pos, 0.001f, 1 << gameObject.layer, QueryTriggerInteraction.Collide).Contains(col);
        }
    }
}

