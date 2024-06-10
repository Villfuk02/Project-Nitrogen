using System;
using System.IO;
using Data.WorldGen;
using UnityEngine;
using Utils;

namespace WorldGen.Decorations
{
    public class ScattererVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] SpriteRenderer sr;
        [Header("Settings")]
        [SerializeField] Gradient gradient;
        [Header("Runtime controls")]
        [SerializeField] int pixelsPerUnit = 1;
        [SerializeField] int decorationIndex = -1;
        [SerializeField] bool saveAsFile;
        [Header("Runtime variables")]
        Texture2D tex_;
        Color32[] cols_;
        int lastIndex_ = -1;
        int lastPixelsPerUnit_;

        void Update()
        {
            if (saveAsFile)
            {
                saveAsFile = false;
                Save();
            }

            if (!sr.enabled || (lastIndex_ == decorationIndex && lastPixelsPerUnit_ == pixelsPerUnit))
                return;
            if (decorationIndex >= 0 && decorationIndex < WorldGenerator.TerrainType.ScattererData.decorations.Length)
                DisplayField(WorldGenerator.TerrainType.ScattererData.decorations[decorationIndex]);
            else
                Clear();
            lastIndex_ = decorationIndex;
            lastPixelsPerUnit_ = pixelsPerUnit;
        }

        void DisplayField(Decoration decoration)
        {
            Vector2Int texSize = WorldUtils.WORLD_SIZE * pixelsPerUnit;

            if (cols_ is null || cols_.Length != texSize.x * texSize.y)
                cols_ = new Color32[texSize.x * texSize.y];

            foreach (Vector2Int pixel in texSize)
                cols_[pixel.x + pixel.y * texSize.x] = EvaluatePixel(pixel, decoration);

            if (tex_ == null || tex_.width != texSize.x || tex_.height != texSize.y)
                ResetTexture(texSize);

            tex_.SetPixels32(cols_);
            tex_.Apply();
        }

        Color32 EvaluatePixel(Vector2Int pixel, Decoration decoration)
        {
            Vector2 tilePos = (Vector2.one * 0.5f + pixel) / pixelsPerUnit - Vector2.one * 0.5f;
            float value = new DecorationEvaluator(tilePos).Evaluate(decoration);
            return gradient.Evaluate(1 / (1 + Mathf.Exp(-value)));
        }

        void ResetTexture(Vector2Int size)
        {
            tex_ = new(size.x, size.y)
            {
                filterMode = FilterMode.Point
            };
            sr.sprite = Sprite.Create(tex_, new(Vector2.zero, size), Vector2.one * 0.5f, pixelsPerUnit);
        }

        void Clear()
        {
            tex_ = null;
            sr.sprite = null;
        }

        void Save()
        {
            if (tex_ == null)
                return;
            var dirPath = Application.dataPath + "/../Exports/";
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            File.WriteAllBytes(dirPath + DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + ".png", tex_.EncodeToPNG());
        }
    }
}