using System.Collections.Generic;
using BattleSimulation.Buildings;
using BattleSimulation.World;
using BattleVisuals.Selection.Highlightable;
using UnityEngine;
using Utils;

namespace BattleVisuals.Selection
{
    public abstract class BuildingAffectingHighlightProvider : TilePlacementHighlightProvider
    {
        public override IEnumerable<(IHighlightable, HighlightType)> GetHighlights()
        {
            if (!TryGetTile(out var tile))
                yield break;

            yield return (tile, HighlightType.Selected);

            foreach (var otherTile in Tiles.TILES)
            {
                if (otherTile == tile || !IsTileAffected(otherTile.pos))
                    continue;
                if (otherTile.Building == null || !IsBuildingAffected(otherTile.Building))
                    continue;
                yield return (otherTile, HighlightType.Affected);
            }
        }

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            float radius = GetRadiusToTileEdge(baseWorldPos);
            if (b == null || !TryGetTile(out _) || !IsTileAffected(((Vector2)WorldUtils.WorldPosToTilePos(baseWorldPos)).Round()))
            {
                var (highlight, rad) = base.GetAffectedArea(baseWorldPos);
                return (highlight, Mathf.Min(radius, rad));
            }

            return (HighlightType.Affected, radius);
        }

        public abstract bool IsTileAffected(Vector2Int tile);
        public abstract bool IsBuildingAffected(Building b);
    }
}