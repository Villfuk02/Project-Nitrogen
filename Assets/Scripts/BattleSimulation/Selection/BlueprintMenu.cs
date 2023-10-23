using BattleSimulation.Control;
using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Selection
{
    public class BlueprintMenu : MonoBehaviour
    {
        public Blueprint[] blueprints;
        public int[] cooldowns;
        public int selected;

        void Start()
        {
            cooldowns = new int[blueprints.Length];
            for (int i = 0; i < blueprints.Length; i++)
            {
                cooldowns[i] = blueprints[i].startingCooldown;
                blueprints[i] = blueprints[i].Clone();
            }
            WaveController.onWaveFinished.Register(ReduceCooldowns, 10);
        }

        void OnDestroy()
        {
            WaveController.onWaveFinished.Unregister(ReduceCooldowns);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
                FinishCooldowns();
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

            float cost = blueprint.cost;
            if (!BattleController.canSpendMaterial.Query(ref cost))
                return false;

            selected = index;
            return true;
        }

        public bool OnPlace()
        {
            if (!BattleController.AdjustAndTrySpendMaterial(blueprints[selected].cost))
                return false;
            cooldowns[selected] = blueprints[selected].cooldown;
            return true;
        }

        public void ReduceCooldowns()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                if (cooldowns[i] > 0)
                    cooldowns[i]--;
            }
        }

        void FinishCooldowns()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i] = 0;
            }
        }
    }
}
