using UnityEngine;

namespace BattleVisuals.Abilities
{
    public class Meteor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BattleSimulation.Abilities.Meteor sim;
        [SerializeField] GameObject body;
        [SerializeField] ParticleSystem flightParticles;
        [SerializeField] ParticleSystem explosionParticles;
        [Header("Settings")]
        [SerializeField] float startHeight;
        [Header("Runtime variables")]
        Vector3 speed_;
        bool stopped_;
        void Start()
        {
            var t = transform;
            t.localPosition = (Vector3.up + Random.onUnitSphere * 0.3f) * startHeight;
            speed_ = -t.localPosition * 20 / sim.Blueprint.delay;
        }

        void Update()
        {
            if (!sim.placed || stopped_)
                return;
            if (sim.delayLeft > 0)
            {
                transform.Translate(Time.deltaTime * speed_, Space.Self);
                return;
            }
            stopped_ = true;
            transform.localPosition = Vector3.zero;
            body.SetActive(false);
            flightParticles.Stop();
            explosionParticles.Play();
        }
    }
}
