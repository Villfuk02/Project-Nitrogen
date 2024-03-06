using BattleSimulation.Control;
using Game.AttackerStats;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.UI
{
    public class WaveBatchDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject attackerIconPrefab;
        [SerializeField] WavesDisplay wd;
        [SerializeField] TextMeshProUGUI countText;
        [Header("Settings")]
        [SerializeField] Vector2 baseSpacing;
        [SerializeField] Vector2 padding;
        [Header("Runtime variables")]
        [SerializeField] float shift;
        readonly List<List<GameObject>> icons_ = new();
        int count_;
        float spacingMultiplier_;

        public void Init(WaveGenerator.Batch batch, WavesDisplay wd)
        {
            this.wd = wd;
            count_ = batch.count;
            countText.text = $"{count_}x";
            spacingMultiplier_ = batch.spacing.GetDisplaySpacing();
            int pathCount = batch.typePerPath.Length;
            float greatestWidth = InitPaths(batch, pathCount);
            countText.rectTransform.anchoredPosition = Vector2.up * (pathCount * baseSpacing.y + padding.y);
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, greatestWidth);
        }

        float InitPaths(WaveGenerator.Batch batch, int pathCount)
        {
            float greatestWidth = CalculateWidth(1);
            int visCount = Mathf.Min(batch.spacing.GetMaxDisplayCount(), batch.count);
            for (int p = 0; p < pathCount; p++)
            {
                if (batch.typePerPath[p] == null)
                    continue;

                greatestWidth = Mathf.Max(greatestWidth, InitPath(batch, visCount, p));
            }
            return greatestWidth;
        }

        float InitPath(WaveGenerator.Batch batch, int visCount, int path)
        {
            for (int i = 0; i < visCount; i++)
            {
                GameObject ai = Instantiate(attackerIconPrefab, transform);
                ai.GetComponent<Image>().sprite = batch.typePerPath[path]!.icon;
                RectTransform rt = (RectTransform)ai.transform;
                rt.anchoredPosition = new(padding.x + baseSpacing.x * i * spacingMultiplier_ + shift * path, padding.y + baseSpacing.y * path);
                if (icons_.Count <= i)
                    icons_.Add(new());
                icons_[i].Add(ai);
            }

            return CalculateWidth(visCount);
        }

        float CalculateWidth(int iconCount) => padding.x * 2 + baseSpacing.x * (iconCount - 1) * spacingMultiplier_;

        public bool SpawnedOnce()
        {
            count_--;
            countText.text = $"{count_}x";
            if (count_ < icons_.Count)
            {
                foreach (var ai in icons_[count_])
                    Destroy(ai);

                icons_.RemoveAt(count_);
                ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, CalculateWidth(count_));
                wd.ForceUpdate();
            }
            if (count_ == 0)
            {
                Destroy(gameObject);
                return true;
            }
            return false;
        }
    }
}
