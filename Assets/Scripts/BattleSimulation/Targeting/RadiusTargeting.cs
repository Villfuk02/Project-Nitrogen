using UnityEngine;

namespace BattleSimulation.Targeting
{
    public class RadiusTargeting : Targeting
    {
        [Header("References")]
        [SerializeField] CapsuleCollider radiusTrigger;
        [SerializeField] BoxCollider heightTrigger;
        [Header("Settings")]
        public bool canTargetDownwards;
        public bool canTargetUpwards;

        protected override void InitComponents()
        {
            targetingComponent = new IntersectionTargetingComponent(
                radiusTrigger.GetComponent<TargetingCollider>(),
                heightTrigger.GetComponent<TargetingCollider>()
            );
        }

        public override void SetRange(float range)
        {
            base.SetRange(range);

            radiusTrigger.radius = range;
            radiusTrigger.height = 10 + range * 2;

            float startHeight = canTargetDownwards ? -5 : 0.1f;
            float endHeight = canTargetUpwards ? 5 : 0.6f;
            heightTrigger.size = new(2 * range, endHeight - startHeight, 2 * range);
            heightTrigger.center = (startHeight + endHeight) * 0.5f * Vector3.up;
        }
    }
}