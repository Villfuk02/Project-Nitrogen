using System.Collections.Generic;
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

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            float range = currentBlueprint.range;
            if (targeting.currentRange != range)
                targeting.SetRange(range);
        }

        public override IEnumerable<string> GetExtraStats()
        {
            if (damageDealt > 0)
                yield return $"Damage dealt [#DMG]{damageDealt}";

            foreach (string s in base.GetExtraStats())
                yield return s;
        }
    }
}