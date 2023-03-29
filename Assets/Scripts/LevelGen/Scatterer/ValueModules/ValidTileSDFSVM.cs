using System;
using UnityEngine;
using static Assets.Scripts.LevelGen.LevelGenerator;

namespace Assets.Scripts.LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public class ValidTileSDFSVM : SDFSVM
    {
        [SerializeField] int[] validBlockers;
        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            return ScaledResult(pos, (p) =>
            {
                for (int i = 0; i < validBlockers.Length; i++)
                {
                    if (Tiles[p].blocker == validBlockers[i])
                        return true;
                }
                return false;
            });
        }
    }
}
