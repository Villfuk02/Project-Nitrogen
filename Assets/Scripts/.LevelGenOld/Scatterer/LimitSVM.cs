using System;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGenOld.Scatterer
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
                if (svm != null)
                    ret += svm.EvaluateAt(pos, som);
            }
            if (ret == float.NegativeInfinity)
                return float.NegativeInfinity;
            return Mathf.Clamp(ret, min, max);
        }
    }
}