using BattleSimulation.Attackers;
using BattleSimulation.World;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Placement
{
    public abstract class Placement : MonoBehaviour
    {
        public UnityEvent onPlaced;
        public PlacementState state;
        public abstract void Setup(PlacementState state);
        public abstract bool IsValid();
        public abstract IEnumerable<Attacker> GetAffectedAttackers();
        public abstract IEnumerable<Tile> GetAffectedTiles();
        public abstract Predicate<Vector3> GetAffectedRegion();
        public abstract void Place();
    }
}
