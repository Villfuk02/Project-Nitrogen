using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Damage;
using UnityEngine;
using Utils;

namespace BattleSimulation.Towers
{
    public class Predator : BasicProjectileTower
    {
        [Header("Runtime variables - Predator")]
        public int kills;

        protected override void OnPlaced()
        {
            base.OnPlaced();
            Attacker.DIE.RegisterReaction(OnAttackerDied, 1000);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
                Attacker.DIE.UnregisterReaction(OnAttackerDied);
        }

        void OnAttackerDied((Attacker attacker, Damage cause) param)
        {
            if (param.cause.source != (object)this)
                return;

            kills++;
            Blueprint.damage++;
        }

        public override IEnumerable<string> GetExtraStats()
        {
            if (Placed)
                yield return $"Kills {kills}";

            foreach (string s in base.GetExtraStats())
                yield return s;
        }
    }
}
