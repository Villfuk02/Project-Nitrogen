using UnityEngine;

namespace BattleSimulation.Selection
{
    public class GlobalPlacement : Placement
    {
        bool initialized_;

        public override bool Setup(Selectable selected, int rotation, Vector3? pos, Transform defaultParent)
        {
            if (initialized_)
                return false;

            initialized_ = true;
            return true;
        }

        public override bool IsValid()
        {
            return true;
        }

        public override bool IsCorrectTypeSelected()
        {
            return false;
        }
    }
}