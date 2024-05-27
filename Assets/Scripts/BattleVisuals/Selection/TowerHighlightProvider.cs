using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Towers;
using BattleSimulation.World;
using BattleVisuals.Selection.Highlightable;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class TowerHighlightProvider : HighlightProvider
    {
        [Header("References")]
        [SerializeField] Tower t;
        [SerializeField] HighlightProvider placementHighlightProvider;
        public override int AreaSamplesPerFrame => 128;

        public override IEnumerable<(IHighlightable, HighlightType)> GetHighlights()
        {
            if (!transform.parent.parent.TryGetComponent<Tile>(out var tile))
                yield break;

            yield return (tile, HighlightType.Selected);

            foreach (var a in t.targeting.GetValidTargets())
                yield return (a, HighlightType.Affected);
        }

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            if (!transform.parent.parent.TryGetComponent<Tile>(out _))
                return placementHighlightProvider.GetAffectedArea(baseWorldPos);
            Vector3 smallPos = baseWorldPos + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
            Vector3 largePos = baseWorldPos + Vector3.up * Attacker.LARGE_TARGET_HEIGHT;
            if (!t.targeting.IsInBounds(largePos) || !t.targeting.IsValidTargetPosition(largePos))
                return (t.Placed ? HighlightType.Clear : placementHighlightProvider.GetAffectedArea(baseWorldPos).highlight, 0);
            if (!t.targeting.IsValidTargetPosition(smallPos))
                return (HighlightType.Affected, 0);
            return (HighlightType.Special, 0);
        }
    }
}