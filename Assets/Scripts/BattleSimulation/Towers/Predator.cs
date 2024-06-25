using System.Collections.Generic;
using BattleSimulation.Attackers;
using Game.Blueprint;
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
            Blueprint.Damage.RegisterModifier(UpdateDamage, -1000000);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Placed)
            {
                Attacker.DIE.UnregisterReaction(OnAttackerDied);
                Blueprint.Damage.UnregisterModifier(UpdateDamage);
            }
        }

        void OnAttackerDied((Attacker attacker, Damage cause) param)
        {
            if (!ReferenceEquals(param.cause.source, this))
                return;
            kills++;
            SoundController.PlaySound(SoundController.Sound.Upgrade, 0.4f, 1, 0, transform.position);
        }

        public override IEnumerable<string> GetExtraStats()
        {
            if (Placed)
                yield return $"Kills {kills}";

            foreach (string s in base.GetExtraStats())
                yield return s;
        }

        void UpdateDamage(IBlueprintProvider provider, ref float damage)
        {
            if (provider as Blueprinted == this)
                damage += kills;
        }
    }
}