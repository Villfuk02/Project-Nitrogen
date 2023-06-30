
using System;
using UnityEngine;

namespace LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public class LimitSVM : ScattererValueModule
    {
        public float min = float.NegativeInfinity;
        public float max = float.PositiveInfinity;
        [SerializeReference, SubclassSelector] public ScattererValueModule[] svms;

        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            float ret = 0;
            foreach (ScattererValueModule svm in svms)
            {
                if (svm is not null)
                    ret += svm.EvaluateAt(pos, som);
            }
            if (ret == float.NegativeInfinity)
                return float.NegativeInfinity;
            return Mathf.Clamp(ret, min, max);
        }
    }
}
