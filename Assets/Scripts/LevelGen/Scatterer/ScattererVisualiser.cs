
using UnityEngine;
using Utils;

namespace LevelGen.Scatterer
{
    public class ScattererVisualiser : MonoBehaviour
    {
        [SerializeField] Scatterer scatterer;
        [SerializeField] SpriteRenderer sr;
        [SerializeField] int pixelsPerUnit;
        [SerializeField] Gradient gradient;
        [SerializeField] int module = -1;
        Texture2D tex;
        Color32[] cols;

        private void Update()
        {
            if (sr.enabled && module >= 0 && module < scatterer.SOMSetup.Length)
                DisplayField(scatterer.SOMSetup[module]);
        }

        void DisplayField(ScattererObjectModule m)
        {
            Vector2Int texSize = WorldUtils.WORLD_SIZE * pixelsPerUnit;
            if (cols is null || cols.Length != texSize.x * texSize.y)
                cols = new Color32[texSize.x * texSize.y];
            bool different = false;
            foreach (Vector2Int v in texSize)
            {
                int i = v.x + v.y * texSize.x;
                Vector2 tilePos = (Vector2.one * 0.5f + v) / pixelsPerUnit - Vector2.one * 0.5f;
                float e = m.EvaluateAt(tilePos);
                Color32 c = gradient.Evaluate(1 / (1 + Mathf.Exp(-e)));
                if (c.r != cols[i].r || c.g != cols[i].g || c.b != cols[i].b)
                    different = true;
                cols[i] = c;
            }
            if (tex is null)
            {
                tex = new(texSize.x, texSize.y)
                {
                    filterMode = FilterMode.Point
                };
                Sprite sp = Sprite.Create(tex, new Rect(Vector2.zero, texSize), Vector2.one * 0.5f, pixelsPerUnit);
                sr.sprite = sp;
            }
            if (different)
            {
                tex.SetPixels32(cols);
                tex.Apply();
            }

        }
    }
}
