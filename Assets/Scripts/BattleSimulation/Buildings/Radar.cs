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
            GET_BLUEPRINT.RegisterModifier(UpdateStats, -1000);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                GET_BLUEPRINT.UnregisterModifier(UpdateStats);
        }

        void UpdateStats(ref (Blueprinted blueprinted, Blueprint blueprint) param)
        {
            if (param.blueprint.type == Blueprint.Type.Ability || !param.blueprint.HasRange)
                return;
            float distSqr = (param.blueprinted.transform.position.XZ() - transform.position.XZ()).sqrMagnitude;
            if (distSqr > 2.01f)
                return;
            param.blueprint.range *= 1 + rangeIncrease;
        }
    }
}