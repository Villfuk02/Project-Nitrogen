using BattleSimulation.Attackers;
using UnityEngine;
using Utils;

namespace BattleVisuals.Towers
{
    public class HoloSiphon : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BattleSimulation.Towers.HoloSiphon sim;
        [SerializeField] ParticleSystem localParticles;
        [SerializeField] ParticleSystem targetParticles;
        [SerializeField] Transform particleHolder;
        [SerializeField] AudioSource siphonSoundEffect;
        [Header("Settings")]
        [SerializeField] Gradient particleColor;
        [Header("Runtime variables")]
        [SerializeField] bool playingParticles;
        [SerializeField] Attacker prevTarget;

        void Start()
        {
            particleHolder.rotation = Quaternion.identity;
        }

        void Update()
        {
            bool hasTarget = sim.selectedTarget != null;
            if (hasTarget)
            {
                var shape = targetParticles.shape;
                shape.position = sim.selectedTarget.target.position - targetParticles.transform.position;
                if (!playingParticles)
                {
                    targetParticles.Play();
                    playingParticles = true;
                }
            }
            else if (playingParticles)
            {
                targetParticles.Stop();
                playingParticles = false;
            }

            Color c = particleColor.Evaluate(hasTarget ? sim.chargeTimer / (float)sim.currentBlueprint.delay : 0);
            var main = localParticles.main;
            main.startColor = c;
            main = targetParticles.main;
            main.startColor = c;

            if (hasTarget && sim.selectedTarget != prevTarget)
            {
                siphonSoundEffect.pitch = siphonSoundEffect.clip.length / (sim.currentBlueprint.delay * TimeUtils.SECS_PER_TICK);
                siphonSoundEffect.Play();
            }
            else if (!hasTarget || sim.selectedTarget != prevTarget)
            {
                siphonSoundEffect.Stop();
            }

            prevTarget = sim.selectedTarget;
        }
    }
}