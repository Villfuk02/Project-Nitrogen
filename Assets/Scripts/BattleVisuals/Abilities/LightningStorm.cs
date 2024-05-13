using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BattleVisuals.Abilities
{
    public class LightningStorm : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BattleSimulation.Abilities.LightningStorm sim;
        [SerializeField] ParticleSystem particles;
        [SerializeField] LineRenderer boltRenderer;
        [Header("Settings")]
        [SerializeField] float boltDuration;
        [SerializeField] Gradient boltColor;
        [SerializeField] Light lightEffect;
        [SerializeField] AnimationCurve lightIntensity;
        [SerializeField] float particlesMultiplier;
        [SerializeField] int lightningBoltSteps;
        [Header("Runtime variables")]
        bool stopped_;
        float boltTimer_;
        float radius_;

        void Start()
        {
            radius_ = Mathf.Max(0.25f, sim.Blueprint.radius);
            var particlesShape = particles.shape;
            particlesShape.scale = new(radius_, 0.25f, radius_);
            var particlesEmission = particles.emission;
            particlesEmission.rateOverTimeMultiplier = particlesMultiplier * radius_ * radius_;
            particlesEmission.SetBurst(0, new(0, 3 * particlesMultiplier * radius_ * radius_));
        }
        void Update()
        {
            if (!sim.Placed)
                return;
            UpdateBolt();
            boltTimer_ += Time.deltaTime;

            if (sim.strikes > 0 || stopped_)
                return;
            stopped_ = true;
            particles.Stop();
        }

        void UpdateBolt()
        {
            float t = Mathf.Clamp01(boltTimer_ / boltDuration);
            Color c = boltColor.Evaluate(t);
            Gradient g = new();
            g.SetKeys(new[] { new GradientColorKey(c, 0) }, Array.Empty<GradientAlphaKey>());
            boltRenderer.colorGradient = g;
            lightEffect.intensity = lightIntensity.Evaluate(t);
        }

        public void Strike(Transform a)
        {
            boltTimer_ = 0;
            Vector3 startPos = a.position;
            Vector3 endPos = transform.position + Vector3.Scale(Random.insideUnitSphere, new(radius_, 0.25f, radius_));
            var pos = new Vector3[lightningBoltSteps + 1];
            pos[0] = startPos;
            pos[lightningBoltSteps] = endPos;
            for (int i = 1; i < lightningBoltSteps; i++)
            {
                Vector2 rand = Random.insideUnitCircle * 2;
                Vector3 dir = (endPos - pos[i - 1]) / (lightningBoltSteps + 1 - i);
                Vector3 offset = new Vector3(rand.x, 1, rand.y) * dir.magnitude;
                offset = Vector3.Lerp(offset, dir, i / (float)lightningBoltSteps);
                pos[i] = pos[i - 1] + offset;
            }
            boltRenderer.positionCount = lightningBoltSteps + 1;
            boltRenderer.SetPositions(pos);
            UpdateBolt();
        }
    }
}
