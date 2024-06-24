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
            Blueprint.Damage.RegisterModifier(UpdateDamage, -1000);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                Blueprint.Damage.UnregisterModifier(UpdateDamage);
        }

        void UpdateDamage(IBlueprintProvider provider, ref float damage)
        {
            if (provider is not Blueprinted blueprinted || provider.GetBaseBlueprint().type != Blueprint.Type.Tower || !provider.GetBaseBlueprint().HasDamage)
                return;
            float distSqr = (blueprinted.transform.position.XZ() - transform.position.XZ()).sqrMagnitude;
            float maxDist = currentBlueprint.range;
            if (distSqr > maxDist * maxDist)
                return;
            damage += damageIncrease;
        }
    }
}