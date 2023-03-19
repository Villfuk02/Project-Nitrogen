using System;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public class ConstantSVM : ScattererValueModule
    {
        public float constant;
        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            return constant;
        }
    }
}