using Assets.Scripts.Attackers.Simulation;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Buildings.Simulation.Towers
{
    public class Targetting : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TargettingCollider[] parts;
        [Header("Runtime values")]
        public Attacker? target;

        private void Awake()
        {
            foreach (var part in parts)
            {
                part.targetting = this;
            }
        }

        public void TargetFound()
        {
            if (target == null)
                Retarget();
        }
        public void TargetLost()
        {
            target = null;
            Retarget();
        }
        private void Retarget()
        {
            if (parts.Length == 0)
                return;
            HashSet<Attacker> valid = new(parts[0].inRange);
            for (int i = 1; i < parts.Length; i++)
            {
                valid.IntersectWith(parts[i].inRange);
            }
            if (valid.Count > 0)
            {
                float min = float.PositiveInfinity;
                foreach (Attacker attacker in valid)
                {
                    if (attacker == null)
                        continue;
                    float dist = attacker.GetDistanceToCenter();
                    if (dist < min)
                    {
                        min = dist;
                        target = attacker;
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (parts.Length == 0)
                return;
            HashSet<Attacker> valid = new(parts[0].inRange);
            HashSet<Attacker> all = new(parts[0].inRange);
            for (int i = 1; i < parts.Length; i++)
            {
                valid.IntersectWith(parts[i].inRange);
                all.UnionWith(parts[i].inRange);
            }
            foreach (Attacker attacker in all)
            {
                if (attacker == null)
                    continue;
                if (attacker == target)
                    Gizmos.color = Color.red;
                else if (valid.Contains(attacker))
                    Gizmos.color = Color.yellow;
                else
                    continue;
                Gizmos.DrawLine(transform.position, attacker.transform.position);
            }
        }
    }
}

