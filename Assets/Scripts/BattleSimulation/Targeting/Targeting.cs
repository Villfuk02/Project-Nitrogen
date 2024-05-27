using System.Collections.Generic;
using System.Linq;
using BattleSimulation.Attackers;
using Game.Shared;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace BattleSimulation.Targeting
{
    public abstract class Targeting : MonoBehaviour, ITargetingParent
    {
        [Header("References")]
        protected ITargetingChild targetingComponent;
        [Header("Settings")]
        [SerializeField] protected bool checkLineOfSight;

        [SerializeField] TargetingPriority.Set availablePriorities;
        protected TargetingPriority[] Priorities { get; private set; }
        protected int selectedPriority;
        public bool CanChangePriority => Priorities.Length > 1;
        public string CurrentPriority => Priorities.Length > 0 ? Priorities[selectedPriority].Name : "";
        [SerializeField] UnityEvent<Attacker> onTargetFound;
        [SerializeField] UnityEvent<Attacker> onTargetLost;
        [Header("Runtime values")]
        public Attacker target;
        protected HashSet<Attacker> inRange = new();

        void Awake()
        {
            Priorities = availablePriorities.ToArray();
            InitComponents();
            targetingComponent.SetParent(this);
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
            onTargetFound.Invoke(t);
        }

        public void TargetLost(Attacker t)
        {
            inRange.Remove(t);
            if (target == t)
                DropCurrentTarget();
            onTargetLost.Invoke(t);
        }

        public void DropCurrentTarget()
        {
            target = null;
            Retarget();
        }

        bool IsValidTarget(Attacker t)
        {
            return t != null && !t.IsDead && IsValidTargetPosition(t.target.position);
        }

        public virtual bool IsValidTargetPosition(Vector3 pos)
        {
            return !checkLineOfSight || HasLineOfSight(pos);
        }

        public void Retarget()
        {
            var validTargets = GetValidTargets().EmptyToNull();
            if (Priorities.Length == 0)
                target = validTargets?.First();
            else
                target = validTargets?.ArgMax(a => Priorities[selectedPriority].GetPriority(a, transform.position));
        }

        bool HasLineOfSight(Vector3 pos)
        {
            var position = transform.position;
            Vector3 dir = pos - position;
            return !Physics.Raycast(position, dir, out RaycastHit _, dir.magnitude, LayerMasks.coarseTerrainAndObstacles);
        }

        public bool IsInBounds(Vector3 pos) => targetingComponent != null && targetingComponent.IsInBounds(pos);
        public abstract void SetRange(float range);

        public IEnumerable<Attacker> GetValidTargets()
        {
            return inRange.Where(a => a != null && IsValidTarget(a));
        }

        public bool IsAmongTargets(Attacker a) => a != null && IsValidTarget(a) && inRange.Contains(a);

        public bool IsInRangeAndValid(Attacker a)
        {
            return a != null && inRange.Contains(a) && IsValidTarget(a);
        }

        public void NextPriority()
        {
            if (!CanChangePriority)
                return;
            selectedPriority = (selectedPriority + 1) % Priorities.Length;
        }

        public void PrevPriority()
        {
            if (!CanChangePriority)
                return;
            selectedPriority = MathUtils.Mod(selectedPriority - 1, Priorities.Length);
        }

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