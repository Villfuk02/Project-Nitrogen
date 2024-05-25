using System.Collections.Generic;
using BattleSimulation.Attackers;
using Game.Damage;
using Game.Shared;
using UnityEngine;

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
            if (!ReferenceEquals(param.cause.source, this))
                return;
            kills++;
            baseBlueprint.damage++;
            SoundController.PlaySound(SoundController.Sound.Upgrade, 0.4f, 1, 0, transform.position, false);
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