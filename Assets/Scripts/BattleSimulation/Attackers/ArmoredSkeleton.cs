using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace BattleSimulation.Attackers
{
    public class ArmoredSkeleton : Attacker
    {
        [Header("Armored Skeleton")]
        [SerializeField] int damageReduction;
        [SerializeField] UnityEvent onShieldDropped;
        [SerializeField] bool hasShield;

        static ArmoredSkeleton()
        {
            DAMAGE.RegisterModifier(TryBlockDamage, -1);
            DAMAGE.RegisterReaction(OnDamaged, 1000);
            SPEED.RegisterModifier(UpdateSpeed, -1000000);
        }

        static bool TryBlockDamage(ref (Attacker target, Damage damage) param)
        {
            if (param.target is not ArmoredSkeleton { hasShield: true } s || param.damage.type.HasFlag(Damage.Type.HealthLoss))
                return true;

            param.damage.amount -= s.damageReduction;
            return param.damage.amount > 0;
        }

        static void OnDamaged((Attacker target, Damage damage) param)
        {
            if (param.target is not ArmoredSkeleton { hasShield: true } s || s.health >= (s.stats.maxHealth + 1) / 2)
                return;

            s.hasShield = false;
            s.onShieldDropped.Invoke();
        }

        static void UpdateSpeed(Attacker attacker, ref float speed)
        {
            if (attacker is ArmoredSkeleton { hasShield: false })
                speed *= 2;
        }
    }
}