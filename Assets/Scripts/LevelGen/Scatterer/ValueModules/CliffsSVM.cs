using Assets.Scripts.Utils;
using System;
using UnityEngine;
using static Assets.Scripts.LevelGen.LevelGenerator;

namespace Assets.Scripts.LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public class CliffsSVM : ScattererValueModule
    {
        public float radius;
        public float maxDrop;
        public float maxRise;
        public float penaltyMultiplier;
        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            float baseHeight = Tiles.GetHeightAt(pos).Value;
            float minHeight = baseHeight;
            float maxHeight = baseHeight;
            for (int i = 0; i < 4; i++)
            {
                Vector2 samplePoint = pos + ((Vector2)WorldUtils.CARDINAL_DIRS[i]) * radius;
                float? hh = Tiles.GetHeightAt(samplePoint);
                if (hh.HasValue)
                {
                    if (hh > maxHeight)
                        maxHeight = hh.Value;
                    else if (hh < minHeight)
                        minHeight = hh.Value;
                }
            }
            float ret = 0;
            if (baseHeight - minHeight > maxDrop)
            {
                ret += baseHeight - minHeight - maxDrop;
            }
            if (maxHeight - baseHeight > maxRise)
            {
                ret += maxHeight - baseHeight - maxRise;
            }
            if (ret == 0)
                return 0;
            return ret * penaltyMultiplier;
        }
    }
}