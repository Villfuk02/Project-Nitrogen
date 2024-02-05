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
        [SerializeField] GameObject attackerIconPrefab;
        [SerializeField] WavesDisplay wd;
        [SerializeField] TextMeshProUGUI countText;
        [SerializeField] Vector2 baseSpacing;
        [SerializeField] Vector2 padding;
        [SerializeField] float shift;
        readonly List<List<GameObject>> icons_ = new();
        int count_;
        float spacing_;

        public void Init(WaveGenerator.Batch batch, WavesDisplay wd)
        {
            this.wd = wd;
            countText.text = $"{batch.count}x";
            int pathCount = batch.typePerPath.Length;
            float maxWidth = padding.x * 2;
            count_ = batch.count;
            spacing_ = batch.spacing.GetDisplaySpacing();
            int visCount = Mathf.Min(batch.spacing.GetDisplayAmount(), batch.count);
            for (int p = 0; p < pathCount; p++)
            {
                if (batch.typePerPath[p] == null)
                    continue;
                for (int i = 0; i < visCount; i++)
                {
                    GameObject ai = Instantiate(attackerIconPrefab, transform);
                    ai.GetComponent<Image>().sprite = batch.typePerPath[p].icon;
                    RectTransform rt = (RectTransform)ai.transform;
                    rt.anchoredPosition = new(padding.x + baseSpacing.x * i * spacing_ + shift * p, padding.y + baseSpacing.y * p);
                    if (icons_.Count <= i)
                        icons_.Add(new());
                    icons_[i].Add(ai);
                }
                float w = padding.x * 2 + baseSpacing.x * (visCount - 1) * spacing_;
                maxWidth = Mathf.Max(maxWidth, w);
            }
            countText.rectTransform.anchoredPosition = Vector2.up * (pathCount * baseSpacing.y + padding.y);
            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
        }

        public bool SpawnedOnce()
        {
            count_--;
            countText.text = $"{count_}x";
            if (count_ < icons_.Count)
            {
                foreach (var ai in icons_[count_])
                {
                    Destroy(ai);
                }
                icons_.RemoveAt(count_);
                ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, padding.x * 2 + baseSpacing.x * (count_ - 1) * spacing_);
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
