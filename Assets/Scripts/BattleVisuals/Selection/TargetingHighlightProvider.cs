using BattleSimulation.Attackers;
using BattleSimulation.Targeting;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class TargetingHighlightProvider : HighlightProvider
    {
        [SerializeField] protected Targeting t;
        [SerializeField] bool highlightAttackers;
        [SerializeField] protected HighlightProvider placementHighlightProvider;
        public override int AreaSamplesPerFrame => 128;
        public override IEnumerable<(IHighlightable, HighlightType)> GetHighlights()
        {
            if (!highlightAttackers)
                yield break;

            foreach (var a in t.GetValidTargets())
                yield return (a, HighlightType.Affected);
        }

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            Vector3 smallPos = baseWorldPos + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
            Vector3 largePos = baseWorldPos + Vector3.up * Attacker.LARGE_TARGET_HEIGHT;
            if (!t.IsInBounds(largePos) || !t.IsValidTargetPosition(largePos))
                return (placementHighlightProvider.GetAffectedArea(baseWorldPos).highlight, 0);
            if (!t.IsValidTargetPosition(smallPos))
                return (HighlightType.Affected, 0);
            return (HighlightType.Special, 0);
        }
    }
}
