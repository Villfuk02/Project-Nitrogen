using BattleSimulation.Buildings;
using BattleSimulation.Selection;
using BattleSimulation.World;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace BattleVisuals.Selection
{
    public class TilePlacementHighlightProvider : HighlightProvider
    {
        [SerializeField] Building b;
        [SerializeField] TilePlacement placement;
        public override int AreaSamplesPerFrame => 400;
        public override IEnumerable<(IHighlightable, HighlightType)> GetHighlights()
        {
            if (!transform.parent.TryGetComponent<Tile>(out var tile))
                yield break;

            yield return (tile, HighlightType.Selected);
        }

        public override (HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            if (b != null && b.placed)
                return (HighlightType.Clear, float.PositiveInfinity);
            Vector3 tilePos = WorldUtils.WorldPosToTilePos(baseWorldPos);
            Vector3Int tile = tilePos.Round();
            Vector3 offset = tilePos - tile;
            float dist = 0.5f - Mathf.Min(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            bool valid = Tiles.TILES.TryGet((Vector2Int)tile, out var selectedTile);
            valid &= placement.IsTileValid(selectedTile);
            return (valid ? HighlightType.Selected : HighlightType.Clear, 2 * dist);
        }
    }
}
