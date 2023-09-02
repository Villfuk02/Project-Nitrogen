
using Attackers.Simulation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace Buildings.Simulation.Towers.Targeting
{
    public abstract class Targeting : MonoBehaviour, ITargetingParent
    {
        [Header("References")]
        public Tower tower;
        protected ITargetingChild targetingComponent;
        // Constants
        protected static LayerMask visibilityMask;
        static bool layerMaskInit_;
        [Header("Settings")]
        [SerializeField] protected bool checkLineOfSight;
        protected abstract TargetingPriority[] Priorities { get; }
        [Header("Runtime values")]
        [SerializeField] int selectedPriority;
        public Attacker target;
        protected HashSet<Attacker> inRange = new();

        void Awake()
        {
            if (!layerMaskInit_)
            {
                visibilityMask = LayerMask.GetMask(LayerNames.COARSE_TERRAIN, LayerNames.COARSE_BLOCKER);
                layerMaskInit_ = true;
            }
            InitComponents();
            targetingComponent.InitParent(this);
        }

        protected abstract void InitComponents();

        void FixedUpdate()
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

        public void Retarget()
        {
            var validTargets = inRange.Where(a => a != null && IsValidTarget(a)).EmptyToNull();
            if (Priorities.Length == 0)
                target = validTargets?.First();
            else
                target = validTargets?.ArgMax(a => Priorities[selectedPriority].GetPriority(a, tower));
        }

        bool HasLineOfSight(Vector3 pos)
        {
            var position = transform.position;
            Vector3 dir = pos - position;
            return !Physics.Raycast(position, dir, out RaycastHit _, dir.magnitude, visibilityMask);
        }

        public abstract List<IEnumerator<Vector2>> GetRangeOutline();

        public bool IsInBounds(Vector3 pos) => targetingComponent != null && targetingComponent.IsInBounds(pos);
        public abstract void SetRange(float range);

        void OnDrawGizmosSelected()
        {
            foreach (Attacker attacker in inRange)
            {
                if (!IsValidTarget(attacker))
                    continue;
                Gizmos.color = attacker == target ? Color.red : Color.yellow;
                Gizmos.DrawLine(transform.position, attacker.target.position);
            }
        }

    }
}

