using BattleSimulation.Buildings;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public abstract class BuildingPlacement : TilePlacement
    {
        [SerializeField] protected Building b;
        public override void Place()
        {
            selectedTile.building = b;
            b.Placed();
            base.Place();
        }
    }
}
