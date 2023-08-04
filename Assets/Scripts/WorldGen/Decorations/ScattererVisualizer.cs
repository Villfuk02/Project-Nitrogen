using Data.WorldGen;
using UnityEngine;
using Utils;

namespace WorldGen.Decorations
{
    public class ScattererVisualizer : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sr;
        [SerializeField] int pixelsPerUnit;
        [SerializeField] Gradient gradient;
        [SerializeField] int module = -1;
        Texture2D tex_;
        Color32[] cols_;

        void Update()
        {
            if (sr.enabled && module >= 0 && module < WorldGenerator.TerrainType.ScattererData.decorations.Length)
                DisplayField(WorldGenerator.TerrainType.ScattererData.decorations[module]);
        }

        void DisplayField(Decoration decoration)
        {
            Vector2Int texSize = WorldUtils.WORLD_SIZE * pixelsPerUnit;
            if (cols_ is null || cols_.Length != texSize.x * texSize.y)
                cols_ = new Color32[texSize.x * texSize.y];
            bool different = false;
            foreach (Vector2Int v in texSize)
            {
                int i = v.x + v.y * texSize.x;
                Vector2 tilePos = (Vector2.one * 0.5f + v) / pixelsPerUnit - Vector2.one * 0.5f;
                float e = new DecorationEvaluator(tilePos).Evaluate(decoration);
                Color32 c = gradient.Evaluate(1 / (1 + Mathf.Exp(-e)));
                if (c.r != cols_[i].r || c.g != cols_[i].g || c.b != cols_[i].b)
                    different = true;
                cols_[i] = c;
            }
            if (tex_ == null)
            {
                tex_ = new(texSize.x, texSize.y)
                {
                    filterMode = FilterMode.Point
                };
                Sprite sp = Sprite.Create(tex_, new(Vector2.zero, texSize), Vector2.one * 0.5f, pixelsPerUnit);
                sr.sprite = sp;
            }
            if (different)
            {
                tex_.SetPixels32(cols_);
                tex_.Apply();
            }

        }
    }
}
