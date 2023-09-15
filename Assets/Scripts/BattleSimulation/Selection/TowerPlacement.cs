using BattleSimulation.Towers;
using BattleSimulation.World;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class TowerPlacement : Placement
    {
        [SerializeField] Tower t;
        [SerializeField] bool onSlants;
        [SerializeField] Tile selectedTile;

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
            if (selectedTile == null || selectedTile.building != null || selectedTile.obstacle != Tile.Obstacle.None)
                return false;
            if (!onSlants && selectedTile.slant != WorldUtils.Slant.None)
                return false;
            return true;
        }

        public override void Place()
        {
            selectedTile.building = t;
            t.Placed();
            base.Place();
        }
    }
}
