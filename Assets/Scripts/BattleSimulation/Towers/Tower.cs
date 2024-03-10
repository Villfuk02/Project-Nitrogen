using BattleSimulation.Buildings;
using UnityEngine;

namespace BattleSimulation.Towers
{
    public abstract class Tower : Building
    {
        [Header("References")]
        public Targeting.Targeting targeting;
        [Header("Runtime variables")]
        protected int damageDealt;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.range);
        }

        public override string? GetExtraStats() => $"Damage dealt [#DMG]{damageDealt}";
    }
}
