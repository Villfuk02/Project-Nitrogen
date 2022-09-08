using UnityEngine;

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
    float[] amplitudes;
    float[] frequencies;

    void Init()
    {
        offsets = new Vector2[octaves];
        amplitudes = new float[octaves];
        frequencies = new float[octaves];
        float a = baseAmplitude;
        float f = baseFrequency;
        for (int i = 0; i < octaves; i++)
        {
            offsets[i] = Random.insideUnitCircle * 1000;
            amplitudes[i] = a;
            a *= amplitudeMult;
            frequencies[i] = f;
            f *= frequencyMult;
        }
    }

    public float EvaluateAt(Vector2 pos)
    {
        if (offsets == null)
        {
            Init();
        }
        float ret = bias;
        for (int i = 0; i < octaves; i++)
        {
            ret += amplitudes[i] * GetNormalizedNoiseAt(offsets[i] + pos * frequencies[i]);
        }
        return ret;
    }

    static float GetNormalizedNoiseAt(Vector2 pos)
    {
        return Mathf.PerlinNoise(pos.x, pos.y) * 2 - 1;
    }
}
