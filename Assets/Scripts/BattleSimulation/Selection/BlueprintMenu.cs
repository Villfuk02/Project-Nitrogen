using BattleSimulation.Control;
using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public class BlueprintMenu : MonoBehaviour
    {
        [SerializeField] BattleController bc;
        public Blueprint[] blueprints;
        public int[] cooldowns;
        public int selected;

        void Start()
        {
            cooldowns = new int[blueprints.Length];
        }

        public void Deselect()
        {
            selected = -1;
        }

        public bool TrySelect(int index, out Blueprint blueprint)
        {
            blueprint = null;
            if (index < 0 || index >= blueprints.Length)
                return false;

            blueprint = blueprints[index];

            if (cooldowns[index] > 0)
                return false;

            if (blueprint.cost > bc.material)
                return false;

            selected = index;
            return true;
        }

        public void OnPlace()
        {
            bc.material -= blueprints[selected].cost;
            cooldowns[selected] = blueprints[selected].cooldown;
        }

        public void ReduceCooldowns()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                if (cooldowns[i] > 0)
                    cooldowns[i]--;
            }
        }
    }
}
