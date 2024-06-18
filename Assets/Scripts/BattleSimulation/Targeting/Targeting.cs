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
        protected HashSet<Attacker> attackersInRange = new();
        protected List<Attacker> partiallySortedAttackersInRange = new();

        void Awake()
        {
            Priorities = availablePriorities.ToArray();
            InitComponents();
            targetingComponent.SetParent(this);
        }

        protected abstract void InitComponents();

        void FixedUpdate()
        {
            if (attackersInRange.Count > 0)
                Retarget();
            else if (!IsValidTarget(target))
                DropCurrentTarget();
        }

        public void TargetFound(Attacker t)
        {
            if (attackersInRange.Add(t))
                partiallySortedAttackersInRange.Add(t);
            if (target == null)
                Retarget();
            onTargetFound.Invoke(t);
        }

        public void TargetLost(Attacker t)
        {
            attackersInRange.Remove(t);
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
            UpdatePartiallySortedAttackers();
            target = partiallySortedAttackersInRange.FirstOrDefault(IsValidTarget);
        }

        bool HasLineOfSight(Vector3 pos)
        {
            var position = transform.position;
            return !Physics.Linecast(position, pos, LayerMasks.coarseTerrainAndObstacles);
        }

        public bool IsInBounds(Vector3 pos) => targetingComponent != null && targetingComponent.IsInBounds(pos);
        public abstract void SetRange(float range);

        public IEnumerable<Attacker> GetValidTargets()
        {
            return attackersInRange.Where(a => a != null && IsValidTarget(a));
        }

        public bool IsAmongTargets(Attacker a) => a != null && IsValidTarget(a) && attackersInRange.Contains(a);

        public bool IsInRangeAndValid(Attacker a)
        {
            return a != null && attackersInRange.Contains(a) && IsValidTarget(a);
        }

        (float, float, uint) CalculateAttackerPriority(Attacker a)
        {
            if (Priorities.Length == 0)
                return (0, 0, a.startPathSplitIndex);
            var (x, y) = Priorities[selectedPriority].GetPriority(a, transform.position);
            return (x, y, a.pathSplitIndex);
        }

        void UpdatePartiallySortedAttackers()
        {
            partiallySortedAttackersInRange = partiallySortedAttackersInRange
                .Where(a => a != null && attackersInRange.Contains(a))
                .OrderBy(CalculateAttackerPriority)
                .ToList();
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
            foreach (Attacker attacker in attackersInRange)
            {
                if (!IsValidTarget(attacker))
                    continue;
                Gizmos.color = attacker == target ? Color.red : Color.yellow;
                Gizmos.DrawLine(transform.position, attacker.target.position);
            }
        }
    }
}