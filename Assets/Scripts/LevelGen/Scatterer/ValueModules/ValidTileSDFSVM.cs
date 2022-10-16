using System;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public class ValidTileSDFSVM : SDFSVM
    {
        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            return 0;// ScaledResult(pos, som.validTiles);
        }
    }
}
