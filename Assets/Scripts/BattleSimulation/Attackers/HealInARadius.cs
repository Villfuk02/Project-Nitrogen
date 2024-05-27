using Game.Shared;
using UnityEngine;

namespace BattleSimulation.Attackers
{
    public class HealInARadius : MonoBehaviour
    {
        public int amount;
        public float radius;

        public void Heal()
        {
            SoundController.PlaySound(SoundController.Sound.Heal, 0.6f, 1, 0.2f, transform.position, false);
            var hits = Physics.SphereCastAll(transform.position + Vector3.down * radius, radius, Vector3.up, 2 * radius, LayerMasks.attackerTargets);
            foreach (var hit in hits)
            {
                Attacker a = hit.rigidbody.GetComponent<Attacker>();
                if (!a.IsDead)
                    Attacker.HEAL.Invoke((a, amount));
            }
        }
    }
}