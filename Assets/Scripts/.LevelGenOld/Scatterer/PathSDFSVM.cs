using System;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGenOld.Scatterer
{
    [Serializable]
    public class PathSDFSVM : SDFSVM
    {
        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            return ScaledResult(pos, PathFinalizer.pathTiles);
        }
    }
}
