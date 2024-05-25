using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared
{
    public class MuteButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Image image;
        [SerializeField] Sprite notMutedSprite;
        [SerializeField] Sprite mutedSprite;

        void Start()
        {
            image.sprite = PersistentData.Muted ? mutedSprite : notMutedSprite;
        }

        public void Toggle()
        {
            var muted = !PersistentData.Muted;
            PersistentData.Muted = muted;
            image.sprite = muted ? mutedSprite : notMutedSprite;
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.M))
            {
                ButtonSounds.Click();
                Toggle();
            }
        }
    }
}