using BattleSimulation.Abilities;
using UnityEngine;
using Utils;

namespace BattleSimulation.Selection
{
    public class SimplePointPlacement : Placement
    {
        [Header("References")]
        public Ability ability;
        [Header("Runtime variables")]
        public Vector2? selectedPos = Vector2.zero;
        public override bool Setup(Selectable? selected, int rotation, Vector3? pos, Transform defaultParent)
        {
            if (pos is null)
            {
                if (selectedPos is null)
                    return false;

                transform.localPosition = Vector3.one * 1000;
                selectedPos = null;
                return true;
            }
            var newPos = (Vector2)pos;
            if (selectedPos is Vector2 sp && (sp - newPos).sqrMagnitude < 0.0001f)
                return false;

            transform.localPosition = WorldUtils.TilePosToWorldPos(newPos, World.WorldData.World.data.tiles.GetHeightAt(newPos));
            selectedPos = newPos;
            return true;
        }

        public override bool IsValid() => selectedPos is not null;

        public override void Place()
        {
            ability.Placed();
            base.Place();
        }
    }
}
