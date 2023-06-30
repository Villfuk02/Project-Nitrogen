
using UnityEngine;

namespace Utils
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

        //TODO: FIX
        Vector2[] offsets;
        ThreadSafeRandom _rand = new();
        public FractalNoise(int octaves, float bias, float baseAmplitude, float amplitudeMult, float baseFrequency, float frequencyMult)
        {
            this.octaves = octaves;
            this.bias = bias;
            this.baseAmplitude = baseAmplitude;
            this.amplitudeMult = amplitudeMult;
            this.baseFrequency = baseFrequency;
            this.frequencyMult = frequencyMult;
        }

        void Init()
        {
            offsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                offsets[i] = _rand.InsideUnitCircle() * 1000;
            }
        }

        public float EvaluateAt(Vector2 pos)
        {
            if (offsets is null || offsets.Length != octaves)
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
