using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public class BlueprintMenu : MonoBehaviour
    {
        public Blueprint[] blueprints;
        public int selected;

        public void Deselect()
        {
            selected = -1;
        }

        public Blueprint Select(int index)
        {
            selected = index;
            return blueprints[index];
        }
    }
}
