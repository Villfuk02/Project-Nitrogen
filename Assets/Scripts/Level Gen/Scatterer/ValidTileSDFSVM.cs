using System;
using UnityEngine;

[Serializable]
public class ValidTileSDFSVM : SDFSVM
{
    protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
    {
        return ScaledResult(pos, som.validTiles);
    }
}
