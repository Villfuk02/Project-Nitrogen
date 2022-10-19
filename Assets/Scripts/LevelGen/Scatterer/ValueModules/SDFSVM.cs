using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Scatterer.ValueModules
{
    [Serializable]
    public abstract class SDFSVM : ScattererValueModule
    {
        public float internalMultiplier;
        public float externalMultiplier;

        protected float ScaledResult(Vector2 tilePos, Predicate<Vector2Int> isPosIn)
        {
            float sdf = EvaluateSDF(tilePos, isPosIn);
            if (sdf > 0)
                return externalMultiplier * sdf;
            else
                return -internalMultiplier * sdf;
        }

        float EvaluateSDF(Vector2 tilePos, Predicate<Vector2Int> isPosIn)
        {
            if (isPosIn == null)
                return -Mathf.Min(tilePos.x + 0.5f, tilePos.y + 0.5f, WorldUtils.WORLD_SIZE.x - 0.5f - tilePos.x, WorldUtils.WORLD_SIZE.y - 0.5f - tilePos.y);
            Vector2Int rounded = new(Mathf.RoundToInt(tilePos.x), Mathf.RoundToInt(tilePos.y));
            bool inside = isPosIn(rounded);
            float prevMinDist = float.PositiveInfinity;
            for (int r = 0; r < Mathf.Max(WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y) + 1; r++)
            {
                float minDist = float.PositiveInfinity;
                Vector2Int boundsMin = new(Mathf.Max(rounded.x - r, 0), Mathf.Max(rounded.y - r, 0));
                Vector2Int boundsMax = new(Mathf.Min(rounded.x + r, WorldUtils.WORLD_SIZE.x - 1), Mathf.Min(rounded.y + r, WorldUtils.WORLD_SIZE.y - 1));
                if (boundsMin.x == rounded.x - r)
                {
                    for (int y = boundsMin.y; y <= boundsMax.y; y++)
                    {
                        if (isPosIn(new(boundsMin.x, y)) != inside)
                        {
                            float dist = GetSD(tilePos, new(boundsMin.x, y));
                            if (dist < minDist)
                                minDist = dist;
                        }
                    }
                }
                if (boundsMax.x == rounded.x + r)
                {
                    for (int y = boundsMin.y; y <= boundsMax.y; y++)
                    {
                        if (isPosIn(new(boundsMax.x, y)) != inside)
                        {
                            float dist = GetSD(tilePos, new(boundsMax.x, y));
                            if (dist < minDist)
                                minDist = dist;
                        }
                    }
                }
                if (boundsMin.y == rounded.y - r)
                {
                    for (int x = boundsMin.x; x <= boundsMax.x; x++)
                    {
                        if (isPosIn(new(x, boundsMin.y)) != inside)
                        {
                            float dist = GetSD(tilePos, new(x, boundsMin.y));
                            if (dist < minDist)
                                minDist = dist;
                        }
                    }
                }
                if (boundsMax.y == rounded.y + r)
                {
                    for (int x = boundsMin.x; x <= boundsMax.x; x++)
                    {
                        if (isPosIn(new(x, boundsMax.y)) != inside)
                        {
                            float dist = GetSD(tilePos, new(x, boundsMax.y));
                            if (dist < minDist)
                                minDist = dist;
                        }
                    }
                }
                if (minDist != float.PositiveInfinity)
                {
                    if (prevMinDist != float.PositiveInfinity)
                    {
                        if (prevMinDist < minDist)
                            minDist = prevMinDist;
                        return inside ? -minDist : minDist;
                    }
                    else
                    {
                        prevMinDist = minDist;
                    }
                }
                else if (prevMinDist != float.PositiveInfinity)
                {
                    return inside ? -prevMinDist : prevMinDist;
                }
            }
            return inside ? -1_000_000 : 1_000_000;
        }
        float GetSD(Vector2 pos, Vector2Int tile)
        {
            static float CoordDiff(float pos, float target)
            {
                float diff = pos - target;
                if (diff < -0.5f)
                    return diff + 0.5f;
                if (diff > 0.5f)
                    return diff - 0.5f;
                return 0;
            }
            return new Vector2(CoordDiff(pos.x, tile.x), CoordDiff(pos.y, tile.y)).magnitude;
        }
    }
}