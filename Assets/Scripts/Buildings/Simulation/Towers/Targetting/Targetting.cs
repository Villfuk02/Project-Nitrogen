
using Attackers.Simulation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace Buildings.Simulation.Towers.Targetting
{
    public abstract class Targetting : MonoBehaviour, ITargettingParent
    {
        [Header("References")]
        protected ITargettingChild targettingComponent;
        // Constatnts
        protected static LayerMask visibilityMask;
        static bool layerMaskInit;
        [Header("Settings")]
        [SerializeField] protected bool checkLineOfSight;
        [Header("Runtime values")]
        [SerializeField] protected Attacker target;
        [SerializeField] protected HashSet<Attacker> inRange = new();

        private void Awake()
        {
            if (!layerMaskInit)
            {
                visibilityMask = LayerMask.GetMask(LayerNames.COARSE_TERRAIN, LayerNames.COARSE_BLOCKER);
                layerMaskInit = true;
            }
            InitComponents();
            targettingComponent.InitParent(this);
        }

        protected abstract void InitComponents();
        private void FixedUpdate()
        {
            if (target == null && inRange.Count > 0)
                Retarget();
            else if (!IsValidTarget(target))
                DropCurrentTarget();
        }

        public void TargetFound(Attacker t)
        {
            inRange.Add(t);
            if (target == null)
                Retarget();
        }
        public void TargetLost(Attacker t)
        {
            inRange.Remove(t);
            if (target == t)
                DropCurrentTarget();
        }
        public void DropCurrentTarget()
        {
            target = null;
            Retarget();
        }
        bool IsValidTarget(Attacker t)
        {
            return t != null && IsValidTargetPosition(t.target.position);
        }
        public bool IsValidTargetPosition(Vector3 pos)
        {
            return !checkLineOfSight || HasLineOfSight(pos);
        }
        private void Retarget()
        {
            target = inRange.Where(a => a != null && IsValidTarget(a)).EmptyToNull()?.ArgMin(a => a.GetDistanceToCenter());
        }
        private bool HasLineOfSight(Vector3 pos)
        {
            Vector3 dir = pos - transform.position;
            return !Physics.Raycast(transform.position, dir, out RaycastHit _, dir.magnitude, visibilityMask);
        }

        public abstract List<IEnumerator<Vector2>> GetRangeOutline();

        public bool IsInBounds(Vector3 pos) => targettingComponent != null && targettingComponent.IsInBounds(pos);

        private void OnDrawGizmosSelected()
        {
            foreach (Attacker attacker in inRange)
            {
                if (!IsValidTarget(attacker))
                    continue;
                if (attacker == target)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, attacker.target.position);
            }
        }
    }
}

