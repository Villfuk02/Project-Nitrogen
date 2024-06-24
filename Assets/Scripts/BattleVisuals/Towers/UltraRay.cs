using System.Linq;
using BattleSimulation.Targeting;
using UnityEngine;

namespace BattleVisuals.Towers
{
    public class UltraRay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BattleSimulation.Towers.UltraRay tower;
        [SerializeField] RayTargeting targeting;
        [SerializeField] LineRenderer beam;

        void Update()
        {
            float length = targeting.realRange;

            var hitAttackers = targeting.GetValidTargets().OrderBy(tower.LateralDistance).Take(tower.hits).ToArray();
            if (hitAttackers.Length == tower.hits)
                length = tower.LateralDistance(hitAttackers[^1]);

            beam.SetPosition(2, Vector3.forward * Mathf.Min(length * 0.99f, tower.currentBlueprint.range * 0.7f));
            beam.SetPosition(3, Vector3.forward * length);
        }
    }
}