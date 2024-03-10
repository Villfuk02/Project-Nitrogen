using BattleSimulation.Attackers;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class AttackerHighlightProvider : HighlightProvider
    {
        [Header("References")]
        [SerializeField] Attacker a;
        public override int AreaSamplesPerFrame => 0;

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            return (HighlightType.Selected, float.PositiveInfinity);
        }

        public override IEnumerable<(HighlightType, IHighlightable)> GetHighlights()
        {
            yield return (HighlightType.Selected, a);
        }
    }
}
