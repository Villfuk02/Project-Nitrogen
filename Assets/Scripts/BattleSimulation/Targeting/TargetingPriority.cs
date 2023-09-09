using BattleSimulation.Attackers;
using UnityEngine;

namespace BattleSimulation.Targeting
{
    public record TargetingPriority(string Name, TargetingPriority.GetAttackerPriority GetPriority)
    {
        public delegate float GetAttackerPriority(Attacker attacker, Vector3 pos);

        public static readonly TargetingPriority FIRST = new("First", (a, _) => -a.GetDistanceToCenter());
        public static readonly TargetingPriority LAST = new("Last", (a, _) => a.GetDistanceToCenter());
        public static readonly TargetingPriority CLOSEST = new("Closest", (a, pos) => -(a.transform.position - pos).sqrMagnitude);
        public static readonly TargetingPriority FARTHEST = new("Farthest", (a, pos) => (a.transform.position - pos).sqrMagnitude);
        public static readonly TargetingPriority STRONGEST = new("Strongest", (a, _) => a.health);
        public static readonly TargetingPriority WEAKEST = new("Weakest", (a, _) => -a.health);
    }
}
