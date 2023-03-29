using Assets.Scripts.Attackers.Simulation;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Buildings.Simulation.Towers
{
    public class TargettingCollider : MonoBehaviour
    {
        public Targetting targetting;
        public HashSet<Attacker> inRange = new();

        private void OnTriggerEnter(Collider other)
        {
            other.transform.parent.TryGetComponent(out Attacker attacker);
            if (attacker != null)
            {
                bool first = inRange.Count == 0;
                inRange.Add(attacker);
                if (first)
                    targetting.TargetFound();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            other.transform.parent.TryGetComponent(out Attacker attacker);
            if (attacker != null)
            {
                inRange.Remove(attacker);
                if (targetting.target == attacker)
                    targetting.TargetLost();
            }
        }
    }
}

