using UnityEngine;
using Utils;

namespace BattleSimulation.Control
{
    public class ProtectiveBubble : MonoBehaviour
    {
        [Header("References")]
        public Transform visuals;
        public Collider col;
        [Header("Settings")]
        public float radius;
        public int ticksLeft;
        public float fadeTime;
        [Header("Runtime variables")]
        [SerializeField] float elapsedTime;
        [SerializeField] float timeLeft;

        void Awake()
        {
            WaveController.ON_WAVE_FINISHED.RegisterReaction(Delete, 100);
        }

        void OnDestroy()
        {
            WaveController.ON_WAVE_FINISHED.UnregisterReaction(Delete);
        }

        void Start()
        {
            timeLeft = ticksLeft * TimeUtils.SECS_PER_TICK;
            transform.localScale = Vector3.one * radius;
        }

        void Update()
        {
            elapsedTime += Time.deltaTime;
            timeLeft -= Time.deltaTime;
            if (elapsedTime <= fadeTime)
                visuals.localScale = Vector3.one * (elapsedTime / fadeTime * 2);
            else if (timeLeft <= fadeTime)
                visuals.localScale = Vector3.one * (timeLeft / fadeTime * 2);
            else
                visuals.localScale = Vector3.one * 2;
        }

        void FixedUpdate()
        {
            ticksLeft--;
            if (ticksLeft <= 0)
            {
                col.enabled = false;
                Destroy(gameObject);
            }
        }

        void Delete()
        {
            timeLeft = fadeTime;
            ticksLeft = Mathf.FloorToInt(fadeTime * TimeUtils.TICKS_PER_SEC);
            col.enabled = false;
        }
    }
}