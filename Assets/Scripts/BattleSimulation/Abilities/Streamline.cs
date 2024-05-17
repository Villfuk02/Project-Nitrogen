using System;
using Game.Blueprint;
using UnityEngine;
using Utils;

namespace BattleSimulation.Abilities
{
    public class Streamline : Ability
    {
        [Header("Settings")]
        [SerializeField] int costReduction;
        [SerializeField] float radiusIncrease;
        [SerializeField] float intervalDecrease;
        [SerializeField] int minInterval;
        [SerializeField] float destroyAfter;

        public void Activate()
        {
            IBlueprintHolder blueprintMenu = GameObject.FindGameObjectWithTag(TagNames.BLUEPRINT_MENU).GetComponent<IBlueprintHolder>();

            foreach (var blueprint in blueprintMenu.GetBlueprints())
            {
                if (blueprint == OriginalBlueprint)
                    continue;

                blueprint.materialCost = Math.Max(0, blueprint.materialCost - costReduction);
                blueprint.energyCost = Math.Max(0, blueprint.energyCost - costReduction);

                if (blueprint.HasRadius)
                    blueprint.radius *= 1 + radiusIncrease;
                if (blueprint.HasRange)
                    blueprint.range *= 1 + radiusIncrease;

                if (blueprint.HasInterval && blueprint.interval > minInterval)
                    blueprint.interval = Mathf.Max(Mathf.FloorToInt(blueprint.interval * (1 - intervalDecrease)), minInterval);

                Destroy(gameObject, destroyAfter);
            }
        }
    }
}