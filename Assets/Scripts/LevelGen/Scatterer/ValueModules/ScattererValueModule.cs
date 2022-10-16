using System;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public abstract class ScattererValueModule
    {
        public bool enabled = true;
        public float EvaluateAt(Vector2 pos, ScattererObjectModule som)
        {
            if (!enabled)
                return 0;
            return EvaluateInternal(pos, som);
        }
        protected abstract float EvaluateInternal(Vector2 pos, ScattererObjectModule som);
    }
}
