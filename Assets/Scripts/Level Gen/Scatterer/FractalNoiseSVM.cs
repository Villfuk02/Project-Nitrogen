using System;
using UnityEngine;

[Serializable]
public class FractalNoiseSVM : ScattererValueModule
{
    public FractalNoise noise;
    protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
    {
        return noise.EvaluateAt(pos);
    }
}
