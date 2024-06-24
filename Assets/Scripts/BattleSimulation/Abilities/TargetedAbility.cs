using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class TargetedAbility : Ability
    {
        [Header("References")]
        [SerializeField] protected Targeting.Targeting targeting;

        protected override void FixedUpdate()
        {
            float radius = currentBlueprint.radius;
            if (targeting.currentRange != radius)
                targeting.SetRange(radius);

            base.FixedUpdate();
        }
    }
}