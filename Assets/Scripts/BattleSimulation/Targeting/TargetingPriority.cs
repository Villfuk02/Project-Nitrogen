using System;
using System.Collections.Generic;
using System.Linq;
using BattleSimulation.Attackers;
using UnityEngine;

namespace BattleSimulation.Targeting
{
    public record TargetingPriority(string Name, TargetingPriority.GetAttackerPriority GetPriority)
    {
        public delegate (float, float) GetAttackerPriority(Attacker attacker, Vector3 sourcePos);

        [Flags] public enum Set { First = 1 << 0, Last = 1 << 1, Closest = 1 << 2, Farthest = 1 << 3, Weakest = 1 << 4, Strongest = 1 << 5 }

        public static readonly Dictionary<Set, TargetingPriority> ALL = new()
        {
            { Set.First, new("First", (a, _) => (a.GetDistanceToHub(), a.health)) },
            { Set.Last, new("Last", (a, _) => (-a.GetDistanceToHub(), a.health)) },
            { Set.Closest, new("Closest", (a, pos) => ((a.transform.position - pos).sqrMagnitude, a.GetDistanceToHub())) },
            { Set.Farthest, new("Farthest", (a, pos) => (-(a.transform.position - pos).sqrMagnitude, a.GetDistanceToHub())) },
            { Set.Weakest, new("Weakest", (a, _) => (a.health, a.GetDistanceToHub())) },
            { Set.Strongest, new("Strongest", (a, _) => (-a.health, a.GetDistanceToHub())) }
        };
    }

    public static class TargetingPriorityExtensions
    {
        public static TargetingPriority[] ToArray(this TargetingPriority.Set priority)
        {
            return TargetingPriority.ALL.Where(p => priority.HasFlag(p.Key)).Select(p => p.Value).ToArray();
        }
    }
}