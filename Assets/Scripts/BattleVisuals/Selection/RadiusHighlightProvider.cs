using BattleSimulation.Targeting;
using BattleVisuals.Selection.Highlightable;
using UnityEngine;
using Utils;

namespace BattleVisuals.Selection
{
    public class RadiusHighlightProvider : TargetingHighlightProvider
    {
        public override int AreaSamplesPerFrame => 400;
        public override (IHighlightable.HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            float dist = (transform.localPosition.XZ() - baseWorldPos.XZ()).magnitude;
            float radius = ((RadiusTargeting)t).currentRange;
            if (dist > radius)
                return (IHighlightable.HighlightType.Selected, dist - radius);
            return (IHighlightable.HighlightType.Special, radius - dist);
        }
    }
}
