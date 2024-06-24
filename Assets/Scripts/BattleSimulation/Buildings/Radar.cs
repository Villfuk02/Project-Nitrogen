using Game.Blueprint;
using UnityEngine;
using Utils;

namespace BattleSimulation.Buildings
{
    public class Radar : Building
    {
        [Header("Settings")]
        [SerializeField] float rangeIncrease;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            Blueprint.Range.RegisterModifier(UpdateRange, -1000);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                Blueprint.Range.UnregisterModifier(UpdateRange);
        }

        void UpdateRange(IBlueprintProvider provider, ref float range)
        {
            if (provider is not Blueprinted blueprinted || provider.GetBaseBlueprint().type == Blueprint.Type.Ability || !provider.GetBaseBlueprint().HasRange)
                return;
            float distSqr = (blueprinted.transform.position.XZ() - transform.position.XZ()).sqrMagnitude;
            if (distSqr > 2.01f)
                return;
            range *= 1 + rangeIncrease;
        }
    }
}