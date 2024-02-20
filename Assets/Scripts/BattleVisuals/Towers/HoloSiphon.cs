using UnityEngine;

namespace BattleVisuals.Towers
{
    public class HoloSiphon : MonoBehaviour
    {
        [SerializeField] BattleSimulation.Towers.HoloSiphon sim;
        [SerializeField] ParticleSystem localParticles;
        [SerializeField] ParticleSystem targetParticles;
        [SerializeField] Transform particleHolder;
        [SerializeField] Gradient particleColor;
        [SerializeField] bool playing;

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
                if (!playing)
                {
                    targetParticles.Play();
                    playing = true;
                }
            }
            else if (playing)
            {
                targetParticles.Stop();
                playing = false;
            }
            Color c = particleColor.Evaluate(hasTarget ? sim.chargeTimer / (float)sim.Blueprint.delay : 0);
            var main = localParticles.main;
            main.startColor = c;
            main = targetParticles.main;
            main.startColor = c;
        }
    }
}
