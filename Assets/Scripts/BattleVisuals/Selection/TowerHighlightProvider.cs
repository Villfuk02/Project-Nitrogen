using BattleSimulation.Attackers;
using BattleSimulation.Towers;
using BattleSimulation.World;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class TowerHighlightProvider : HighlightProvider
    {
        [Header("References")]
        [SerializeField] Tower t;
        public override int AreaSamplesPerFrame => 128;
        public override IEnumerable<(HighlightType, IHighlightable)> GetHighlights()
        {
            if (!transform.parent.TryGetComponent<Tile>(out var tile))
                yield break;

            yield return (HighlightType.Selected, tile);

            foreach (var a in t.targeting.GetValidTargets().Select(attacker => (HighlightType.Affected, (IHighlightable)attacker)))
            {
                yield return a;
            }
        }

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            if (!transform.parent.TryGetComponent<Tile>(out _))
                return (HighlightType.Selected, float.PositiveInfinity);
            Vector3 smallPos = baseWorldPos + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
            Vector3 largePos = baseWorldPos + Vector3.up * Attacker.LARGE_TARGET_HEIGHT;
            if (!t.targeting.IsInBounds(largePos))
                return (HighlightType.Selected, 0);
            if (!t.targeting.IsValidTargetPosition(largePos))
                return (HighlightType.Negative, 0);
            if (!t.targeting.IsValidTargetPosition(smallPos))
                return (HighlightType.Affected, 0);
            return (HighlightType.Special, 0);
        }
    }
}
