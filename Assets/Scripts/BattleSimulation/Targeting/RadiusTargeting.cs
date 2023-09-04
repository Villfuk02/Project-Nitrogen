using System.Collections.Generic;
using UnityEngine;

namespace BattleSimulation.Targeting
{
    public class RadiusTargeting : Targeting
    {
        [Header("References")]
        [SerializeField] CapsuleCollider radiusTrigger;
        [SerializeField] BoxCollider heightTrigger;
        [Header("Stats")]
        public float currentRange;
        public bool canTargetDownwards;
        public bool canTargetUpwards;

        protected override TargetingPriority[] Priorities => new[] { TargetingPriority.FIRST, TargetingPriority.LAST, TargetingPriority.CLOSEST, TargetingPriority.FARTHEST };

        protected override void InitComponents()
        {
            targetingComponent = new IntersectionTargetingComponent(
                radiusTrigger.GetComponent<TargetingCollider>(),
                heightTrigger.GetComponent<TargetingCollider>()
                );
        }

        public override void SetRange(float range)
        {
            currentRange = range;

            radiusTrigger.radius = range;
            radiusTrigger.height = 10 + range * 2;

            float startHeight = canTargetDownwards ? -5 : -0.3f;
            float endHeight = canTargetUpwards ? 5 : 0.4f;
            heightTrigger.size = new(2 * range, endHeight - startHeight, 2 * range);
            heightTrigger.center = (startHeight + endHeight) * 0.5f * Vector3.up;
        }

        public override List<IEnumerator<Vector2>> GetRangeOutline()
        {
            static IEnumerator<Vector2> CircleEnumerator(float radius, int steps)
            {
                for (int i = 0; i <= steps; i++)
                {
                    float angle = i * 2 * Mathf.PI / steps;
                    yield return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
            }
            return new() { CircleEnumerator(currentRange, 180) };
        }
    }
}

