using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public abstract class HighlightProvider : MonoBehaviour
    {
        public abstract int AreaSamplesPerFrame { get; }
        public abstract IEnumerable<(IHighlightable.HighlightType, IHighlightable)> GetHighlights();
        public abstract (IHighlightable.HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos);
    }
}
