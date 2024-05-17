using BattleSimulation.World;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class RotatableTilePlacement : TilePlacement
    {
        [Header("Settings")]
        [SerializeField] int rotations;
        [Header("Runtime variables")]
        public int rotation;

        public override bool Setup(Selectable selected, int rotation, Vector3? pos, Transform defaultParent)
        {
            int rot = MathUtils.Mod(rotation, rotations);
            if (rot != this.rotation)
            {
                this.rotation = rot;
                transform.localEulerAngles = 360f * rotation / rotations * Vector3.up;
                selectedTile = null;
            }

            return base.Setup(selected, rotation, pos, defaultParent);
        }

        public override bool IsTileValid(Tile? tile)
        {
            return tile != null;
        }
    }
}