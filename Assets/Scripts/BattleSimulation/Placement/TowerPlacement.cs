using BattleSimulation.Attackers;
using BattleSimulation.Towers;
using BattleSimulation.World;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace BattleSimulation.Placement
{
    public class TowerPlacement : Placement
    {
        [SerializeField] Tower t;
        [SerializeField] bool onSlants;

        public override void Setup(PlacementState state)
        {
            this.state = state;
            if (state.hoveredTile != null)
                transform.position = state.hoveredTile.transform.position;
        }

        public override bool IsValid()
        {
            if (state.hoveredTile == null || state.hoveredTile.building != null || state.hoveredTile.obstacle != Tile.Obstacle.None)
                return false;
            if (!onSlants && state.hoveredTile.slant != WorldUtils.Slant.None)
                return false;
            return true;
        }

        public override IEnumerable<Attacker> GetAffectedAttackers()
        {
            return t.targeting.GetValidTargets();
        }

        public override IEnumerable<Tile> GetAffectedTiles()
        {
            if (state.hoveredTile != null)
                yield return state.hoveredTile;
        }

        public override Predicate<Vector3> GetAffectedRegion()
        {
            return pos => t.targeting.IsInBounds(pos) && t.targeting.IsValidTargetPosition(pos);
        }

        public override void Place()
        {
            state.hoveredTile.building = t;
            transform.SetParent(state.hoveredTile.transform);
            t.Placed();
        }
    }
}
