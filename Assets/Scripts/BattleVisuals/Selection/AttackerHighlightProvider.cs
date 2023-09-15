using BattleSimulation.Attackers;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class AttackerHighlightProvider : HighlightProvider
    {
        [SerializeField] Attacker a;
        public override int AreaSamplesPerFrame => 0;

        public override (IHighlightable.HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            return (IHighlightable.HighlightType.Negative, float.PositiveInfinity);
        }

        public override IEnumerable<(IHighlightable.HighlightType, IHighlightable)> GetHighlights()
        {
            yield return (IHighlightable.HighlightType.Selected, a);
        }
    }
}
