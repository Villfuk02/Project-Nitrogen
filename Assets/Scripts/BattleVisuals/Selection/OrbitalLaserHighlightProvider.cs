using System.Collections.Generic;
using BattleSimulation.Selection;
using BattleVisuals.Selection.Highlightable;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class OrbitalLaserHighlightProvider : HighlightProvider
    {
        [Header("References")]
        [SerializeField] RotatableTilePlacement placement;
        [Header("Settings")]
        [SerializeField] float beamRadius;
        public override int AreaSamplesPerFrame => 1000;

        public override IEnumerable<(IHighlightable, HighlightType)> GetHighlights()
        {
            yield break;
        }

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            bool travelsInX = placement.rotation % 2 == 1;
            Vector3 offset = transform.position - baseWorldPos;
            float dist = Mathf.Abs(travelsInX ? offset.z : offset.x);
            if (dist <= beamRadius)
                return (HighlightType.Special, beamRadius - dist);

            return (HighlightType.Selected, dist - beamRadius);
        }
    }
}