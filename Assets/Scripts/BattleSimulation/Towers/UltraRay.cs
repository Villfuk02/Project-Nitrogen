using System.Linq;
using BattleSimulation.Attackers;
using Game.Damage;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace BattleSimulation.Towers
{
    public class UltraRay : Tower
    {
        [Header("Settings")]
        [SerializeField] protected UnityEvent onShoot;
        public int hits;
        [Header("Runtime variables")]
        [SerializeField] protected int shotTimer;

        protected void FixedUpdate()
        {
            if (!Placed)
                return;

            shotTimer--;
            if (shotTimer <= 0 && targeting.GetValidTargets().Any())
                Shoot();
        }

        void Shoot()
        {
            shotTimer = Blueprint.interval;
            onShoot.Invoke();
            int hitsLeft = hits;
            foreach (var target in targeting.GetValidTargets().OrderBy(LateralDistance))
            {
                if (hitsLeft <= 0)
                    break;
                Hit(target);
                hitsLeft--;
            }
        }

        public float LateralDistance(Attacker a)
        {
            Vector3 diff = a.target.position - targeting.transform.position;
            return diff.XZ().ManhattanMagnitude();
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
