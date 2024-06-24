using System;
using System.Collections.Generic;
using Game.Blueprint;
using Game.Shared;
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
        [Header("Runtime variables")]
        HashSet<Blueprint> affectedBlueprints_;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            affectedBlueprints_ = new(GameObject.FindGameObjectWithTag(TagNames.BLUEPRINT_MENU).GetComponent<IBlueprintHolder>().GetBlueprints());
            affectedBlueprints_.Remove(originalBlueprint);

            Blueprint.MaterialCost.RegisterModifier(UpdateCost, -100);
            Blueprint.Radius.RegisterModifier(UpdateRadius, -100);
            Blueprint.Range.RegisterModifier(UpdateRange, -100);
            Blueprint.Interval.RegisterModifier(UpdateInterval, -100);
        }

        protected void OnDestroy()
        {
            if (Placed)
            {
                Blueprint.MaterialCost.UnregisterModifier(UpdateCost);
                Blueprint.Radius.UnregisterModifier(UpdateRadius);
                Blueprint.Range.UnregisterModifier(UpdateRange);
                Blueprint.Interval.UnregisterModifier(UpdateInterval);
            }
        }

        public void Activate()
        {
            SoundController.PlaySound(SoundController.Sound.Catalyst, 1, 0.6f, 0.1f, transform.position);
        }

        void UpdateCost(IBlueprintProvider provider, ref float cost)
        {
            if (cost > 0 && provider is Blueprint b && affectedBlueprints_.Contains(b))
                cost = Math.Max(0, cost - costReduction);
        }

        void UpdateRadius(IBlueprintProvider provider, ref float radius)
        {
            if (radius > 0 && provider is Blueprint b && affectedBlueprints_.Contains(b))
                radius *= 1 + radiusIncrease;
        }

        void UpdateRange(IBlueprintProvider provider, ref float range)
        {
            if (range > 0 && provider is Blueprint b && affectedBlueprints_.Contains(b))
                range *= 1 + radiusIncrease;
        }

        void UpdateInterval(IBlueprintProvider provider, ref float interval)
        {
            if (interval > minInterval && provider is Blueprint b && affectedBlueprints_.Contains(b))
                interval = Mathf.Max(Mathf.FloorToInt(interval * (1 - intervalDecrease)), minInterval);
        }
    }
}