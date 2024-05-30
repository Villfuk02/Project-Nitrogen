using BattleSimulation.Towers;
using UnityEngine;
using UnityEngine.Serialization;

namespace BattleVisuals.Towers
{
    public class BasicProjectileTowerVisuals : MonoBehaviour
    {
        static readonly int ShootTrigger = Animator.StringToHash("Shoot");
        [Header("References")]
        [SerializeField] ProjectileTower t;
        [SerializeField] Animator shootAnim;
        [SerializeField] Transform turretPivot;
        [FormerlySerializedAs("rotationSmoothing")]
        [Header("Settings")]
        [SerializeField] float rotationSpeed;
        [SerializeField] float rotationLockRatio;

        void Update()
        {
            if (!t.Placed)
                return;

            if (1 - t.shotTimer / (float)t.Blueprint.interval < rotationLockRatio)
                return;

            var targetRotation = GetTargetRotation();
            turretPivot.rotation = Quaternion.Slerp(turretPivot.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        public void Shoot()
        {
            shootAnim.SetTrigger(ShootTrigger);
            turretPivot.rotation = GetTargetRotation();
        }

        Quaternion GetTargetRotation()
        {
            var target = t.targeting.target;
            var currentTargetVisual = target == null ? null : target.visualTarget;

            if (currentTargetVisual != null)
                return Quaternion.LookRotation(currentTargetVisual.position - turretPivot.position);

            return turretPivot.rotation;
        }
    }
}