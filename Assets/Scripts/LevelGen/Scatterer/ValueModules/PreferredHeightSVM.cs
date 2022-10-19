using System;
using UnityEngine;
using static InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.LevelGenerator;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public class PreferredHeightSVM : ScattererValueModule
    {
        public float preferredHeight;
        public float penaltyMultiplier;
        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            float height = Tiles.GetHeightAt(pos).Value;
            float heightDiff = preferredHeight - height;
            return -penaltyMultiplier * Mathf.Abs(heightDiff);
        }
    }
}
