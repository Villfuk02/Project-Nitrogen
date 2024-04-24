using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Attackers
{
    public class RepeatingEventAttacker : Attacker
    {
        [Header("Repeating event settings")]
        public int ticksPeriod;
        public int ticksLeft;
        public UnityEvent repeatingEvent;

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!IsDead && --ticksLeft <= 0)
            {
                repeatingEvent.Invoke();
                ticksLeft += ticksPeriod;
            }
        }
    }
}
