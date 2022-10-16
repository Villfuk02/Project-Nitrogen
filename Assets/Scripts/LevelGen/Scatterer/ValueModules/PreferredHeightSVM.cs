using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public class PreferredHeightSVM : ScattererValueModule
    {
        public float preferredHeight;
        public float penaltyMultiplier;
        protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
        {
            Vector3 rayOrigin = WorldUtils.TileToWorldPos(pos) + (WorldUtils.MAX_HEIGHT + 1) * WorldUtils.HEIGHT_STEP * Vector3.up;
            Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayOrigin.y + 1, LayerMask.GetMask("CoarseTerrain"));
            float heightDiff = preferredHeight - hit.point.y / WorldUtils.HEIGHT_STEP;
            return -penaltyMultiplier * Mathf.Abs(heightDiff);
        }
    }
}
