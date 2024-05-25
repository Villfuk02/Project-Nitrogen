using UnityEngine;

namespace Game.Shared
{
    public class PlaySound : MonoBehaviour
    {
        [SerializeField] SoundController.Sound sound;
        [SerializeField] float volume;
        [SerializeField] float pitch;
        [SerializeField] float pitchVariance;
        [SerializeField] bool worldSpace;
        [SerializeField] bool highPriority;

        public void Play()
        {
            SoundController.PlaySound(sound, volume, pitch, pitchVariance, worldSpace ? transform.position : null, highPriority);
        }
    }
}