using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGenOld.Scatterer
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
