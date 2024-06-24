using BattleSimulation.Buildings;
using Game.Blueprint;
using UnityEngine;

namespace BattleVisuals.Selection
{
    public class AmplifierHighlightProvider : BuildingAffectingHighlightProvider
    {
        public override bool IsTileAffected(Vector2Int tile)
        {
            TryGetTile(out var myTile);
            float range = b.currentBlueprint.range;
            var offset = tile - myTile.pos;
            return offset.sqrMagnitude <= range * range;
        }

        public override bool IsBuildingAffected(Building b)
        {
            return b.currentBlueprint.type == Blueprint.Type.Tower && b.currentBlueprint.HasDamage;
        }
    }
}