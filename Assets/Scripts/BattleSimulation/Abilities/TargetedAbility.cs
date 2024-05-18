using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class TargetedAbility : Ability
    {
        [Header("References")]
        [SerializeField] protected Targeting.Targeting targeting;

        public override void OnSetupChanged()
        {
            base.OnSetupChanged();
            targeting.SetRange(Blueprint.radius);
        }
    }
}