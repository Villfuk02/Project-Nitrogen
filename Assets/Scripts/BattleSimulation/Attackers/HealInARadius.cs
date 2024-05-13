using UnityEngine;
using Utils;

namespace BattleSimulation.Attackers
{
    public class HealInARadius : MonoBehaviour
    {
        static LayerMask attackerMask_;

        public int amount;
        public float radius;

        void Awake()
        {
            if (attackerMask_ == 0)
                attackerMask_ = LayerMask.GetMask(LayerNames.ATTACKER_TARGET);
        }

        public void Heal()
        {
            var hits = Physics.SphereCastAll(transform.position + Vector3.down * radius, radius, Vector3.up, 2 * radius, attackerMask_);
            foreach (var hit in hits)
            {
                Attacker a = hit.rigidbody.GetComponent<Attacker>();
                if (!a.IsDead)
                    Attacker.HEAL.Invoke((a, amount));
            }
        }
    }
}
