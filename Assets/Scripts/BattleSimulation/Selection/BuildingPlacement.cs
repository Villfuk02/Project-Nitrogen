using BattleSimulation.Buildings;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public abstract class BuildingPlacement : TilePlacement
    {
        [SerializeField] protected Building b;
        public override void Place()
        {
            selectedTile.Building = b;
            b.Placed();
            base.Place();
        }
    }
}
