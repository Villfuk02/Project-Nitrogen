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
        public override IEnumerable<(IHighlightable.HighlightType, IHighlightable)> GetHighlights()
        {
            if (!transform.parent.TryGetComponent<Tile>(out var tile))
                yield break;

            yield return (IHighlightable.HighlightType.Selected, tile);
        }

        public override (IHighlightable.HighlightType highlight, float radius) GetAffectedArea(Vector3 baseWorldPos)
        {
            if (b != null && b.placed)
                return (IHighlightable.HighlightType.Selected, float.PositiveInfinity);
            Vector3 tilePos = WorldUtils.WorldPosToTilePos(baseWorldPos);
            Vector3Int tile = tilePos.Round();
            Vector3 offset = tilePos - tile;
            float dist = 0.5f - Mathf.Min(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            bool valid = Tiles.TILES.TryGet((Vector2Int)tile, out var selectedTile);
            valid &= placement.IsTileValid(selectedTile);
            return (valid ? IHighlightable.HighlightType.Special : IHighlightable.HighlightType.Selected, 2 * dist);
        }
    }
}
