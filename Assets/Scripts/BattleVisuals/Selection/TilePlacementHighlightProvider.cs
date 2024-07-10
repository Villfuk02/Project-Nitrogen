using System.Collections.Generic;
using BattleSimulation.Buildings;
using BattleSimulation.Selection;
using BattleSimulation.World;
using BattleVisuals.Selection.Highlightable;
using UnityEngine;
using Utils;

namespace BattleVisuals.Selection
{
    public class TilePlacementHighlightProvider : PlacementHighlightProvider
    {
        [SerializeField] protected Building b;
        [SerializeField] TilePlacement placement;
        public override int AreaSamplesPerFrame => 400;

        public override IEnumerable<(IHighlightable, HighlightType)> GetHighlights()
        {
            if (!TryGetTile(out var tile))
                yield break;

            yield return (tile, HighlightType.Selected);
        }

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            if (b != null && b.Placed)
                return (HighlightType.Clear, float.PositiveInfinity);
            Vector3Int tile = WorldUtils.WorldPosToTilePos(baseWorldPos).Round();
            bool valid = Tiles.TILES.TryGet((Vector2Int)tile, out var selectedTile);
            valid &= placement.IsTileValid(selectedTile);
            return (valid ? HighlightType.Selected : HighlightType.Clear, GetRadiusToTileEdge(baseWorldPos));
        }

        public static float GetRadiusToTileEdge(Vector3 baseWorldPos)
        {
            Vector3 tilePos = WorldUtils.WorldPosToTilePos(baseWorldPos);
            Vector3Int rounded = tilePos.Round();
            Vector3 offset = tilePos - rounded;
            float dist = 0.5f - Mathf.Min(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            return 2 * dist;
        }

        public bool TryGetTile(out Tile tile)
        {
            if (placement == null)
                transform.parent.parent.TryGetComponent(out tile);
            else
                tile = placement.selectedTile;
            return tile != null;
        }
    }
}