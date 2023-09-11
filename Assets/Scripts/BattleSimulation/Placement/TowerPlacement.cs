using BattleSimulation.Attackers;
using BattleSimulation.Towers;
using BattleSimulation.World;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace BattleSimulation.Placement
{
    public class TowerPlacement : Placement
    {
        [SerializeField] Tower t;
        [SerializeField] bool onSlants;

        public override void Setup(PlacementState placementState)
        {
            this.state = placementState;
            if (placementState.hoveredTile != null)
                transform.position = placementState.hoveredTile.transform.position;
        }

        public override bool IsValid()
        {
            if (state.hoveredTile == null || state.hoveredTile.building != null || state.hoveredTile.obstacle != Tile.Obstacle.None)
                return false;
            if (!onSlants && state.hoveredTile.slant != WorldUtils.Slant.None)
                return false;
            return true;
        }

        public override IEnumerable<(Relation, Attacker)> GetAffectedAttackers()
        {
            if (state.hoveredTile != null)
                return t.targeting.GetValidTargets().Select(attacker => (IsValid() ? Relation.Affected : Relation.Negative, attacker));
            return Enumerable.Empty<(Relation, Attacker)>();
        }

        public override IEnumerable<(Relation, Tile)> GetAffectedTiles()
        {
            if (state.hoveredTile != null)
                yield return (IsValid() ? Relation.Selected : Relation.Negative, state.hoveredTile);
        }

        public override Relation GetAffectedRegion(Vector3 baseWorldPos)
        {
            if (state.hoveredTile == null)
                return Relation.Negative;
            Vector3 smallPos = baseWorldPos + Vector3.up * Attacker.SMALL_TARGET_HEIGHT;
            Vector3 largePos = baseWorldPos + Vector3.up * Attacker.LARGE_TARGET_HEIGHT;
            if (!t.targeting.IsInBounds(largePos))
                return Relation.Negative;
            if (!t.targeting.IsValidTargetPosition(largePos))
                return Relation.Selected;
            if (!t.targeting.IsValidTargetPosition(smallPos))
                return Relation.Affected;
            return Relation.Special;
        }

        public override void Place()
        {
            state.hoveredTile.building = t;
            transform.SetParent(state.hoveredTile.transform);
            t.Placed();
        }
    }
}
