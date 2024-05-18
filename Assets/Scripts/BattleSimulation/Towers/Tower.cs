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

        public override void OnSetupChanged()
        {
            base.OnSetupChanged();
            targeting.SetRange(Blueprint.range);
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