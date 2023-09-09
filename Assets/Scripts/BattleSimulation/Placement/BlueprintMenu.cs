using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Placement
{
    public class BlueprintMenu : MonoBehaviour
    {
        public PlacementController pc;
        public Blueprint[] blueprints;
        public int selected;

        void Update()
        {
            for (int i = 0; i < blueprints.Length; i++)
            {
                KeyCode key = KeyCode.Alpha1 + i;
                if (!Input.GetKeyDown(key))
                    continue;

                if (selected == i)
                    Deselect();
                else
                    Select(i);
            }

            if (selected < 0)
                return;

            if (Input.GetMouseButtonUp(1))
                Deselect();
            else if (Input.GetMouseButtonUp(0))
                Place();
        }

        void Deselect()
        {
            selected = -1;
            pc.Deselect();
        }

        void Select(int index)
        {
            if (selected >= 0)
                pc.Deselect();
            selected = index;
            pc.Select(blueprints[index]);
        }

        void Place()
        {
            if (pc.Place())
                Deselect();
        }
    }
}
