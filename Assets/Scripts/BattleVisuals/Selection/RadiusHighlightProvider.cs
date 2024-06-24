using BattleVisuals.Selection.Highlightable;
using UnityEngine;
using Utils;

namespace BattleVisuals.Selection
{
    public class RadiusHighlightProvider : TargetingHighlightProvider
    {
        public override int AreaSamplesPerFrame => 400;

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            float dist = (transform.localPosition.XZ() - baseWorldPos.XZ()).magnitude;
            float radius = t.currentRange;
            if (dist <= radius)
                return (HighlightType.Special, radius - dist);

            var (placementHighlight, placementHighlightRadius) = placementHighlightProvider.GetAffectedArea(baseWorldPos);
            return (placementHighlight, Mathf.Min(dist - radius, placementHighlightRadius));
        }
    }
}