using BattleSimulation.Abilities;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class SimplePointPlacement : Placement
    {
        [SerializeField] public Ability ability;
        [SerializeField] float deadZone;
        public Vector2? selectedPos = Vector2.zero;
        public override bool Setup(Selectable? selected, int rotation, Vector3? pos, Transform defaultParent)
        {
            if (pos is null)
            {
                transform.localPosition = Vector3.one * 1000;
                if (selectedPos is null)
                    return false;
                selectedPos = null;
                return false;
            }
            var newPos = (Vector2)pos;
            transform.localPosition = WorldUtils.TilePosToWorldPos(newPos, World.WorldData.World.data.tiles.GetHeightAt(newPos));

            if (selectedPos is Vector2 sp && (sp - newPos).sqrMagnitude < deadZone * deadZone)
                return false;

            selectedPos = newPos;
            return true;
        }

        public override bool IsValid()
        {
            return selectedPos is not null;
        }

        public override void Place()
        {
            ability.Placed();
            base.Place();
        }
    }
}
