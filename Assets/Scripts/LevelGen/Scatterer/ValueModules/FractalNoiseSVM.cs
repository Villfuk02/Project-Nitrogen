using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer.ValueModules
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