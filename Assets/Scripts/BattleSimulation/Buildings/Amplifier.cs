using Game.Blueprint;
using UnityEngine;
using Utils;

namespace BattleSimulation.Buildings
{
    public class Amplifier : Building
    {
        [Header("Settings")]
        [SerializeField] int damageIncrease;

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
            if (param.blueprint.type != Blueprint.Type.Tower || !param.blueprint.HasDamage)
                return;
            float distSqr = (param.blueprinted.transform.position.XZ() - transform.position.XZ()).sqrMagnitude;
            float maxDist = Blueprint.range;
            if (distSqr > maxDist * maxDist)
                return;
            param.blueprint.damage += damageIncrease;
        }
    }
}