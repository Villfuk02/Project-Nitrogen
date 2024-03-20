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
            return (HighlightType.Clear, float.PositiveInfinity);
        }

        public override IEnumerable<(IHighlightable, HighlightType)> GetHighlights()
        {
            yield return (a, HighlightType.Selected);
        }
    }
}
