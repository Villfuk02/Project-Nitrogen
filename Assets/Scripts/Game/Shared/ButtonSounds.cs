using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Shared
{
    public class ButtonSounds : MonoBehaviour, IPointerEnterHandler
    {
        Selectable selectable_;

        void Awake()
        {
            if (TryGetComponent(out Selectable selectable))
                selectable_ = selectable;
            if (TryGetComponent(out Button button))
                button.onClick.AddListener(Click);
            if (TryGetComponent(out Toggle toggle))
                toggle.onValueChanged.AddListener(_ => Click());
            if (TryGetComponent(out TMP_InputField input))
                input.onSelect.AddListener(_ => Click());
        }

        public static void Click() => SoundController.PlaySound(SoundController.Sound.ButtonClick, 0.3f, 1, 0, null, false);
        public static void Hover() => SoundController.PlaySound(SoundController.Sound.ButtonSelect, 0.1f, 1, 0, null, false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable_ == null || selectable_.interactable)
                Hover();
        }
    }
}