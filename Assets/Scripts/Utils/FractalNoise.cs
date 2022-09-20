using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.Utils
{
    [System.Serializable]
    public class FractalNoise
    {
        public int octaves;
        public float bias;
        public float baseAmplitude;
        public float amplitudeMult;
        public float baseFrequency;
        public float frequencyMult;

        Vector2[] offsets;

        void Init()
        {
            offsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                offsets[i] = Random.insideUnitCircle * 1000;
            }
        }

        public float EvaluateAt(Vector2 pos)
        {
            if (offsets == null || offsets.Length != octaves)
            {
                Init();
            }
            float ret = bias;
            float a = baseAmplitude;
            float f = baseFrequency;
            for (int i = 0; i < octaves; i++)
            {
                ret += a * GetNormalizedNoiseAt(offsets[i] + pos * f);
                a *= amplitudeMult;
                f *= frequencyMult;
            }
            return ret;
        }

        static float GetNormalizedNoiseAt(Vector2 pos)
        {
            return Mathf.PerlinNoise(pos.x, pos.y) * 2 - 1;
        }
    }
}
