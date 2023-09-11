using BattleSimulation.Attackers;
using BattleSimulation.World;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Placement
{
    public abstract class Placement : MonoBehaviour
    {
        public enum Relation { Selected, Negative, Affected, Special }
        public UnityEvent onPlaced;
        public PlacementState state;
        public abstract void Setup(PlacementState placementState);
        public abstract bool IsValid();
        public abstract IEnumerable<(Relation, Attacker)> GetAffectedAttackers();
        public abstract IEnumerable<(Relation, Tile)> GetAffectedTiles();
        public abstract Relation GetAffectedRegion(Vector3 baseWorldPos);
        public abstract void Place();
    }
}
