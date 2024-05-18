using System.Collections;
using System.Collections.Generic;
using BattleSimulation.Control;
using BattleSimulation.World.WorldData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.UI
{
    public class WavesDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject waveDisplayPrefab;
        [SerializeField] GameObject pathLabelPrefab;
        [SerializeField] WaveController wc;
        [SerializeField] BattleController bc;
        [SerializeField] TextMeshProUGUI waveNumber;
        [SerializeField] TextMeshProUGUI remainingText;
        [SerializeField] RectTransform wavesLayout;
        [SerializeField] RectTransform pathLabelHolder;
        [Header("Settings")]
        [SerializeField] Vector2 labelOffset;
        [SerializeField] float minimumWidth;
        [Header("Runtime variables")]
        readonly List<WaveDisplay> waves_ = new();
        [SerializeField] int displayedUpTo;
        float lastWidth_;
        float offset_;
        int fuelProduction_;

        void Awake()
        {
            BattleController.UPDATE_FUEL_PER_WAVE.RegisterReaction(UpdateFuelIncome, 10);
            BattleController.UPDATE_FUEL_PER_WAVE.Invoke(0);
        }

        void OnDestroy()
        {
            BattleController.UPDATE_FUEL_PER_WAVE.UnregisterReaction(UpdateFuelIncome);
        }

        void Start()
        {
            int paths = World.data.firstPathTiles.Length;
            for (int i = 0; i < paths; i++)
            {
                var label = Instantiate(pathLabelPrefab, pathLabelHolder);
                label.GetComponent<TextMeshProUGUI>().text = ((char)('A' + i)).ToString();
                label.GetComponent<RectTransform>().anchoredPosition = labelOffset * i;
            }
        }

        void Update()
        {
            ForceUpdate();
            waveNumber.text = wc.wave.ToString();
            remainingText.text = GetWavesLeftText();
            if (wavesLayout.rect.width < minimumWidth)
                DisplayNext();

            offset_ = Mathf.Lerp(offset_, 0, Time.deltaTime * 5);
        }

        string GetWavesLeftText()
        {
            if (fuelProduction_ <= 0)
                return "???";
            int remaining = (bc.fuelGoal - bc.fuel + fuelProduction_ - 1) / fuelProduction_;
            return remaining switch
            {
                <= 0 => "<size=28>VICTORY</size>",
                1 => "<size=26>last\n\nwave left</size>",
                _ => $"{remaining}\n<size=26>waves left</size>"
            };
        }

        void DisplayNext()
        {
            displayedUpTo++;
            var wd = Instantiate(waveDisplayPrefab, wavesLayout).GetComponent<WaveDisplay>();
            wd.Init(wc.waveGenerator.GetWave(displayedUpTo), displayedUpTo, this);
            waves_.Add(wd);
            wd.transform.localScale = Vector3.zero;
            StartCoroutine(UnHide(wd));
            ForceUpdate();
        }

        static IEnumerator UnHide(Component wd)
        {
            yield return null;
            yield return null;
            wd.transform.localScale = Vector3.one;
        }

        public void SpawnedOnce()
        {
            if (waves_[0].SpawnedOnce())
                waves_.RemoveAt(0);
        }

        public void ForceUpdate()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(wavesLayout);
            UpdateOffset();
        }

        void UpdateOffset()
        {
            if (wavesLayout.rect.width < lastWidth_)
                offset_ += lastWidth_ - wavesLayout.rect.width;
            lastWidth_ = wavesLayout.rect.width;
            wavesLayout.anchoredPosition = Vector2.right * offset_;
        }

        void UpdateFuelIncome(float income)
        {
            fuelProduction_ = (int)income;
        }
    }
}