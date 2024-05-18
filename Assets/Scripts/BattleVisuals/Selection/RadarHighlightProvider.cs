using BattleSimulation.Buildings;
using Game.Blueprint;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class RadarHighlightProvider : BuildingAffectingHighlightProvider
    {
        public override bool IsTileAffected(Vector2Int tile)
        {
            TryGetTile(out var myTile);
            var offset = tile - myTile.pos;
            return offset.sqrMagnitude <= 2.01f;
        }

        public override bool IsBuildingAffected(Building b)
        {
            return b.Blueprint.type != Blueprint.Type.Ability && b.Blueprint.HasRange;
        }
    }
}