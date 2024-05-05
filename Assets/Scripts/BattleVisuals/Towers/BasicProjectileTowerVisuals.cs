using BattleSimulation.Attackers;
using BattleSimulation.Towers;
using UnityEngine;

namespace BattleVisuals.Towers
{
    public class BasicProjectileTowerVisuals : MonoBehaviour
    {
        static readonly int ShootTrigger = Animator.StringToHash("Shoot");
        [Header("References")]
        [SerializeField] BasicProjectileTower t;
        [SerializeField] Animator shootAnim;
        [SerializeField] Transform turretPivot;
        [Header("Runtime variables")]
        [SerializeField] Attacker currentTarget;
        [SerializeField] Transform currentTargetVisual;

        void Update()
        {
            if (!t.placed)
                return;

            var target = t.targeting.target;
            if (target != currentTarget)
            {
                currentTarget = target;
                currentTargetVisual = target == null ? null : target.GetComponent<VisualsReference>().visuals;
            }

            if (currentTargetVisual != null)
            {
                turretPivot.LookAt(currentTargetVisual);
            }
        }

        public void Shoot()
        {
            shootAnim.SetTrigger(ShootTrigger);
        }
    }
}