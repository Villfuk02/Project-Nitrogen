using BattleSimulation.Control;
using BattleSimulation.World.WorldData;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.UI
{
    public class WavesDisplay : MonoBehaviour
    {
        [SerializeField] GameObject waveDisplayPrefab;
        [SerializeField] GameObject pathLabelPrefab;
        [SerializeField] WaveController wc;
        [SerializeField] BattleController bc;
        [SerializeField] TextMeshProUGUI waveNumber;
        [SerializeField] TextMeshProUGUI remainingText;
        [SerializeField] RectTransform wavesLayout;
        [SerializeField] RectTransform pathLabelHolder;
        [SerializeField] Vector2 labelOffset;
        readonly List<WaveDisplay> waves_ = new();

        [SerializeField] float minimumWidth;
        [SerializeField] int displayedUpTo;
        float lastWidth_;
        float offset_;
        int fuelProduction_;
        void Awake()
        {
            BattleController.updateFuelPerWave.Register(UpdateFuelIncome, 10);
            BattleController.updateFuelPerWave.Invoke(0);
        }

        void OnDestroy()
        {
            BattleController.updateFuelPerWave.Unregister(UpdateFuelIncome);
        }

        void Start()
        {
            int paths = World.data.firstPathTiles.Length;
            for (int i = 0; i < paths; i++)
            {
                var lgo = Instantiate(pathLabelPrefab, pathLabelHolder);
                lgo.GetComponent<TextMeshProUGUI>().text = ((char)('A' + i)).ToString();
                lgo.GetComponent<RectTransform>().anchoredPosition = labelOffset * i;
            }
        }

        void Update()
        {
            ForceUpdate();
            waveNumber.text = wc.wave.ToString();
            int remain;
            if (fuelProduction_ <= 0)
                remain = -1;
            else
                remain = (bc.FuelGoal - bc.Fuel + fuelProduction_ - 1) / fuelProduction_;
            remainingText.text = remain switch
            {
                <= 0 => "<size=36>departing</size>",
                1 => "<size=26>last\n\nwave left</size>",
                _ => $"{remain}\n<size=26>waves left</size>"
            };
            if (wavesLayout.rect.width < minimumWidth)
                DisplayNext();

            offset_ = Mathf.Lerp(offset_, 0, Time.deltaTime * 5);
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

        bool UpdateFuelIncome(ref float income)
        {
            fuelProduction_ = (int)income;
            return true;
        }
    }
}
