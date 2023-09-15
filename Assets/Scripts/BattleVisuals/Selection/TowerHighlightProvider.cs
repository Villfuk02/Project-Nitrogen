using BattleSimulation.Attackers;
using BattleSimulation.Towers;
using BattleSimulation.World;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class TowerHighlightProvider : HighlightProvider
    {
        [SerializeField] Tower t;
        public override int AreaSamplesPerFrame => 128;
        public override IEnumerable<(IHighlightable.HighlightType, IHighlightable)> GetHighlights()
        {
            if (!transform.parent.TryGetComponent<Tile>(out var tile))
                yield break;

            yield return (IHighlightable.HighlightType.Selected, tile);

            foreach (var a in t.targeting.GetValidTargets().Select(attacker => (IHighlightable.HighlightType.Affected, (IHighlightable)attacker)))
            {
                yield return a;
            }
        }

        public override (IHighlightable.HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            if (!transform.parent.TryGetComponent<Tile>(out _))
                return (IHighlightable.HighlightType.Negative, float.PositiveInfinity);
            Vector3 smallPos = baseWorldPos + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
            Vector3 largePos = baseWorldPos + Vector3.up * Attacker.LARGE_TARGET_HEIGHT;
            if (!t.targeting.IsInBounds(largePos))
                return (IHighlightable.HighlightType.Negative, 0);
            if (!t.targeting.IsValidTargetPosition(largePos))
                return (IHighlightable.HighlightType.Selected, 0);
            if (!t.targeting.IsValidTargetPosition(smallPos))
                return (IHighlightable.HighlightType.Affected, 0);
            return (IHighlightable.HighlightType.Special, 0);
        }
    }
}
