using System.Linq;
using BattleSimulation.Attackers;
using Game.Damage;
using UnityEngine;
using UnityEngine.Events;

namespace BattleSimulation.Towers
{
    public class Sledgehammer : Tower {
        [Header("Settings")]
        [SerializeField] protected UnityEvent onShoot;
        [SerializeField] protected int shotDelay;
        [Header("Runtime variables")]
        [SerializeField] protected int shotTimer;
        [SerializeField] protected int shotDelayTimer;

        protected void FixedUpdate()
        {
            if (!Placed)
                return;

            shotTimer--;
            shotDelayTimer++;
            if (shotTimer <= 0 && targeting.GetValidTargets().Any())
                StartShot();

            if (shotDelayTimer == shotDelay)
                Shoot();
        }

        void StartShot()
        {
            shotDelayTimer = 0;
            shotTimer = Blueprint.interval;
            onShoot.Invoke();
        }

        void Shoot()
        {
            foreach (var target in targeting.GetValidTargets())
                Hit(target);
        }

        public bool Hit(Attacker attacker)
        {
            if (attacker.IsDead)
                return false;
            (Attacker a, Damage dmg) hitParam = (attacker, new(Blueprint.damage, Blueprint.damageType, this));
            if (!Attacker.HIT.InvokeRef(ref hitParam))
                return false;
            if (hitParam.dmg.amount > 0)
            {
                (Attacker a, Damage dmg) dmgParam = hitParam;
                if (Attacker.DAMAGE.InvokeRef(ref dmgParam))
                    damageDealt += (int)dmgParam.dmg.amount;
            }

            return true;
        }
    }
}
