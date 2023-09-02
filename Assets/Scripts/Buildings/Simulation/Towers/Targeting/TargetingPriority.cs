using Attackers.Simulation;

namespace Buildings.Simulation.Towers.Targeting
{
    public record TargetingPriority(string Name, TargetingPriority.GetAttackerPriority GetPriority)
    {
        public delegate float GetAttackerPriority(Attacker attacker, Tower tower);

        public static readonly TargetingPriority FIRST = new("First", (a, _) => -a.GetDistanceToCenter());
        public static readonly TargetingPriority LAST = new("Last", (a, _) => a.GetDistanceToCenter());
        public static readonly TargetingPriority CLOSEST = new("Closest", (a, t) => -(a.transform.position - t.transform.position).sqrMagnitude);
        public static readonly TargetingPriority FARTHEST = new("Farthest", (a, t) => (a.transform.position - t.transform.position).sqrMagnitude);
    }
}
