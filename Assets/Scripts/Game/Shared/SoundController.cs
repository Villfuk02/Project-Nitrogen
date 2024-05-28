using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared
{
    public class SoundController : MonoBehaviour
    {
        public enum Sound
        {
            ImpactSmall, ImpactBig, ShootProjectile, ChargeUp, EnergizedImpact, ShootHeavy, ExplosionSmall, ExplosionBig, ImpactHuge, Zap,
            RayBurn, Upgrade, Build, Error, HullLoss, Siren, Catalyst, Fall, SiphonFinish, LaserBurn,
            Heal, AttackerDie, Clink, Materials, Energy, Fuel, Victory, Defeat, ButtonClick, ButtonSelect,
            Notification, WaveStart
        }

        public enum Priority { Low = 80, Normal = 64, High = 48 }

        static SoundController instance_;

        [System.Serializable]
        struct ClipArray
        {
            public AudioClip[] clips;
            public AudioClip GetRandom() => clips[Random.Range(0, clips.Length)];
        }

        [Header("References")]
        [SerializeField] GameObject audioSourcePrefab;
        [Header("Settings")]
        [SerializeField] ClipArray[] sounds;
        [Header("Runtime variables")]
        [SerializeField] List<AudioSource> idleSources;

        void Awake()
        {
            instance_ = this;
            DontDestroyOnLoad(gameObject);
            for (int i = 0; i < 20; i++)
            {
                var source = Instantiate(audioSourcePrefab, transform).GetComponent<AudioSource>();
                idleSources.Add(source);
            }

            PersistentData.Muted = PersistentData.Muted;
        }

        public static void PlaySound(Sound sound, float volume, float pitch, float pitchVariance, Vector3? position, Priority priority = Priority.Normal)
        {
            pitch *= Mathf.Pow(1 + pitchVariance, Random.Range(-1f, 1f));
            if (position is not null)
                instance_.PlayWorldSound(sound, volume, pitch, position.Value, priority);
            else
                instance_.PlayScreenSound(sound, volume, pitch, priority);
        }

        void PlayWorldSound(Sound sound, float volume, float pitch, Vector3 position, Priority priority)
        {
            transform.position = position;
            var source = GetSource();
            source.clip = sounds[(int)sound].GetRandom();
            source.spatialBlend = 1;
            source.volume = volume;
            source.pitch = pitch;
            source.priority = (int)priority + 64;
            source.Play();
            StartCoroutine(ReturnSource(source, source.clip.length / pitch + 0.05f));
        }

        void PlayScreenSound(Sound sound, float volume, float pitch, Priority priority)
        {
            transform.localPosition = Vector3.zero;
            var source = GetSource();
            source.clip = sounds[(int)sound].GetRandom();
            source.spatialBlend = 0;
            source.volume = volume;
            source.pitch = pitch;
            source.priority = (int)priority;
            source.Play();
            StartCoroutine(ReturnSource(source, source.clip.length / pitch + 0.05f));
        }

        AudioSource GetSource()
        {
            if (idleSources.Count == 0)
                return Instantiate(audioSourcePrefab, transform).GetComponent<AudioSource>();
            var source = idleSources[^1];
            idleSources.RemoveAt(idleSources.Count - 1);
            return source;
        }

        IEnumerator ReturnSource(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            idleSources.Add(source);
        }
    }
}