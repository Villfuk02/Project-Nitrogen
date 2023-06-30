
using System;
using UnityEngine;
using static LevelGen.LevelGenerator;

namespace LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public class PathSDFSVM : SDFSVM
    {
        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            return ScaledResult(pos, (p) => Tiles[p].dist != int.MaxValue);
        }
    }
}
