using BattleSimulation.World;
using UnityEngine;

namespace BattleSimulation.Placement
{
    [System.Serializable]
    public struct PlacementState
    {
        public Tile hoveredTile;
        public Vector3 tilePos;
        public int rotation;
    }
}
