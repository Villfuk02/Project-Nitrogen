using Game.Shared;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Attackers
{
    public class ArmoredSkeleton : Attacker
    {
        [Header("ArmoredSkeleton")]
        [SerializeField] int damageReduction;
        [SerializeField] UnityEvent onShieldDropped;
        [SerializeField] bool hasShield;

        public virtual void Awake()
        {
            DAMAGE.RegisterModifier(TryBlockDamage, -1);
            DAMAGE.RegisterReaction(OnDamaged, 1000);
        }

        public void OnDestroy()
        {
            DAMAGE.UnregisterModifier(TryBlockDamage);
            DAMAGE.UnregisterReaction(OnDamaged);
        }

        void DropShield()
        {
            hasShield = false;
            onShieldDropped.Invoke();
            stats.speed *= 2;
        }

        bool TryBlockDamage(ref (Attacker target, Damage damage) param)
        {
            if (param.target != this || !hasShield || param.damage.type.HasFlag(Damage.Type.HealthLoss))
                return true;
            param.damage.amount -= damageReduction;
            return param.damage.amount > 0;
        }

        void OnDamaged((Attacker target, Damage damage) param)
        {
            if (hasShield && health < (stats.maxHealth + 1) / 2)
                DropShield();
        }
    }
}