
using System;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public class FractalNoise
    {
        public int octaves;
        public float bias;
        public float baseAmplitude;
        public float amplitudeMult;
        public float baseFrequency;
        public float frequencyMult;

        Vector2[] offsets_;
        public FractalNoise(int octaves, float bias, float baseAmplitude, float amplitudeMult, float baseFrequency, float frequencyMult)
        {
            this.octaves = octaves;
            this.bias = bias;
            this.baseAmplitude = baseAmplitude;
            this.amplitudeMult = amplitudeMult;
            this.baseFrequency = baseFrequency;
            this.frequencyMult = frequencyMult;
        }
        /// <summary>
        /// Initialize the layer offsets to random values, based on the provided seed.
        /// </summary>
        public void Init(ulong randomSeed)
        {
            Random.Random random = new(randomSeed);
            offsets_ = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                offsets_[i] = random.InsideUnitCircle() * 10000;
            }
        }
        /// <summary>
        /// Get the value of the noise at a given point. Throws if the offsets have not been initialized.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public float EvaluateAt(Vector2 pos)
        {
            if (offsets_ is null || offsets_.Length != octaves)
                throw new InvalidOperationException("Noise has not been initialized");

            float ret = bias;
            float a = baseAmplitude;
            float f = baseFrequency;
            for (int i = 0; i < octaves; i++)
            {
                ret += a * GetNormalizedNoiseAt(offsets_[i] + pos * f);
                a *= amplitudeMult;
                f *= frequencyMult;
            }
            return ret;
        }
        // Modifies the Perlin noise values to be in the range [-1..1] instead of [0..1]
        static float GetNormalizedNoiseAt(Vector2 pos)
        {
            return Mathf.PerlinNoise(pos.x, pos.y) * 2 - 1;
        }
    }
}
