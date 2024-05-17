using BattleSimulation.Buildings;
using BattleSimulation.World;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public abstract class TilePlacement : Placement
    {
        [Header("Runtime variables")]
        public Tile selectedTile;

        public override bool Setup(Selectable selected, int rotation, Vector3? pos, Transform defaultParent)
        {
            var newSelectedTile = selected == null ? null : selected.tile;

            if (newSelectedTile == selectedTile)
                return false;

            Transform t = transform;
            if (newSelectedTile != null)
            {
                if (t.TryGetComponent<Building>(out var b))
                {
                    newSelectedTile.SetupBuilding(b);
                }
                else
                {
                    t.localPosition = Vector3.zero;
                    t.SetParent(newSelectedTile.transform, false);
                }
            }
            else
            {
                t.localPosition = Vector3.one * 1000;
                t.SetParent(defaultParent, false);
            }

            selectedTile = newSelectedTile;
            return true;
        }

        public override bool IsValid() => IsTileValid(selectedTile);
        public override bool IsCorrectTypeSelected() => selectedTile != null;

        public abstract bool IsTileValid(Tile? tile);
    }
}