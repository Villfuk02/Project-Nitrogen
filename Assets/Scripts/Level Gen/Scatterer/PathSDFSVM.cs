using System;
using UnityEngine;

[Serializable]
public class PathSDFSVM : SDFSVM
{
    protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
    {
        return ScaledResult(pos, PathFinalizer.pathTiles);
    }
}
