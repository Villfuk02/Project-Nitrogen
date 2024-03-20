using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class PlaceAnywhereHighlightProvider : HighlightProvider
    {
        public override int AreaSamplesPerFrame => 20;
        public override IEnumerable<(IHighlightable, HighlightType)> GetHighlights()
        {
            yield break;
        }

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            return (HighlightType.Selected, float.PositiveInfinity);
        }
    }
}
