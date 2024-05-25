using Game.Shared;
using UnityEngine;
using Utils;

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
            speed_ = -t.localPosition / sim.Blueprint.delay * TimeUtils.TICKS_PER_SEC;
            SoundController.PlaySound(SoundController.Sound.Fall, 0.75f, 0.63f / (sim.Blueprint.delay * TimeUtils.SECS_PER_TICK), 0, transform.position, false);
        }

        void Update()
        {
            if (!sim.Placed || stopped_)
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
            SoundController.PlaySound(SoundController.Sound.ExplosionBig, 0.75f, 1, 0.1f, transform.position, false);
        }
    }
}