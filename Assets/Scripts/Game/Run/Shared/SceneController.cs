using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Run.Shared
{
    public class SceneController : MonoBehaviour
    {
        public enum Scene
        {
            Loading,
            Menu,
            RunSettings,
            Battle,
            BlueprintSelect
        }

        static SceneController instance_;
        [Header("References")]
        [SerializeField] Image overlay;
        [SerializeField] TextMeshProUGUI overlayText;
        [Header("Settings")]
        [SerializeField] float fadeTime = 0.25f;

        void Awake()
        {
            instance_ = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void ChangeScene(Scene scene, bool fadeOut, bool fadeIn, string text = "", Action? fadeOutCallback = null)
        {
            if (fadeOut)
            {
                FadeOut(text, () =>
                {
                    fadeOutCallback?.Invoke();
                    ChangeScene(scene, false, fadeIn, text);
                });
                return;
            }

            SceneManager.LoadScene((int)scene);
            if (fadeIn)
                FadeIn();
        }

        public static void FadeOut(string text = "", Action? callback = null)
        {
            instance_.overlayText.text = text;
            instance_.StartCoroutine(instance_.Fade(1 / instance_.fadeTime, callback));
        }

        public static void FadeIn(Action? callback = null)
        {
            instance_.StartCoroutine(instance_.Fade(-1 / instance_.fadeTime, callback));
        }

        IEnumerator Fade(float speed, Action? callback)
        {
            overlay.enabled = true;
            float alpha = Mathf.Clamp01(overlay.color.a);
            do
            {
                alpha += Time.deltaTime * speed;
                overlay.color = new(0, 0, 0, alpha);
                overlayText.color = new(1, 1, 1, 2 * alpha - 1);
                yield return null;
            } while (alpha is > 0 and < 1);

            if (alpha <= 0)
                overlay.enabled = false;

            callback?.Invoke();
        }
    }
}