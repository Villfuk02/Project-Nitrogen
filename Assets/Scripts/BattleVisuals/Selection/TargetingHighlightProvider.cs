using BattleSimulation.Attackers;
using BattleSimulation.Targeting;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class TargetingHighlightProvider : HighlightProvider
    {
        [SerializeField] protected Targeting t;
        [SerializeField] bool highlightAttackers;
        public override int AreaSamplesPerFrame => 128;
        public override IEnumerable<(HighlightType, IHighlightable)> GetHighlights()
        {
            if (!highlightAttackers)
                yield break;

            foreach (var a in t.GetValidTargets().Select(attacker => (HighlightType.Affected, (IHighlightable)attacker)))
                yield return a;
        }

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            Vector3 smallPos = baseWorldPos + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
            Vector3 largePos = baseWorldPos + Vector3.up * Attacker.LARGE_TARGET_HEIGHT;
            if (!t.IsInBounds(largePos))
                return (HighlightType.Selected, 0);
            if (!t.IsValidTargetPosition(largePos))
                return (HighlightType.Negative, 0);
            if (!t.IsValidTargetPosition(smallPos))
                return (HighlightType.Affected, 0);
            return (HighlightType.Special, 0);
        }
    }
}
