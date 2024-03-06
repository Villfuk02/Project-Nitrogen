using BattleSimulation.World;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class TileHighlightProvider : HighlightProvider
    {
        [SerializeField] Tile t;
        [SerializeField] HighlightProvider buildingHighlightProvider;
        bool HasBuildingHighlightProvider()
        {
            if (t.Building != null && buildingHighlightProvider == null)
                buildingHighlightProvider = t.Building.GetComponent<HighlightProvider>();

            return buildingHighlightProvider != null;
        }
        public override int AreaSamplesPerFrame => HasBuildingHighlightProvider() ? buildingHighlightProvider.AreaSamplesPerFrame : 400;
        public override IEnumerable<(IHighlightable.HighlightType, IHighlightable)> GetHighlights()
        {
            if (HasBuildingHighlightProvider())
                foreach (var r in buildingHighlightProvider.GetHighlights())
                    yield return r;
            else
                yield return (IHighlightable.HighlightType.Selected, t);
        }

        public override (IHighlightable.HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            if (HasBuildingHighlightProvider())
                return buildingHighlightProvider.GetAffectedArea(baseWorldPos);
            return (IHighlightable.HighlightType.Selected, float.PositiveInfinity);
        }
    }
}
