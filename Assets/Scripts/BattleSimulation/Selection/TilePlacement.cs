using BattleSimulation.World;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public abstract class TilePlacement : Placement
    {
        public Tile selectedTile;
        public override bool Setup(Selectable selected, int rotation, Vector3 pos, Transform defaultParent)
        {
            var newSelectedTile = selected == null ? null : selected.tile;

            if (newSelectedTile == selectedTile)
                return false;

            if (newSelectedTile != null)
            {
                Transform myTransform = transform;
                myTransform.SetParent(newSelectedTile.transform);
                myTransform.localPosition = Vector3.zero;
            }
            else
            {
                transform.SetParent(defaultParent);
            }

            selectedTile = newSelectedTile;
            return true;
        }

        public override bool IsValid()
        {
            return IsTileValid(selectedTile);
        }

        public abstract bool IsTileValid(Tile tile);
    }
}
