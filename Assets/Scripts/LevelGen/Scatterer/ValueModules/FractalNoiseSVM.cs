
using System;
using UnityEngine;
using Utils;

namespace LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public class FractalNoiseSVM : ScattererValueModule
    {
        public FractalNoise noise;
        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            return noise.EvaluateAt(pos);
        }
    }
}
