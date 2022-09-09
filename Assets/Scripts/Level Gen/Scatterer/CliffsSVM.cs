using System;
using UnityEngine;

[Serializable]
public class CliffsSVM : ScattererValueModule
{
    public float radius;
    public float maxDrop;
    public float maxRise;
    public float penaltyMultiplier;
    protected override float EvaluateInternal(Vector2 pos, ScattererObjectModule som)
    {
        Vector3 rayOrigin = WorldUtils.TileToWorldPos(pos) + (WorldUtils.MAX_HEIGHT + 1) * WorldUtils.HEIGHT_STEP * Vector3.up;
        Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayOrigin.y + 1, LayerMask.GetMask("CoarseTerrain"));
        float baseHeight = hit.point.y / WorldUtils.HEIGHT_STEP;
        float minHeight = baseHeight;
        float maxHeight = baseHeight;
        for (int i = 0; i < 4; i++)
        {
            Physics.Raycast(rayOrigin + WorldUtils.WORLD_CARDINAL_DIRS[i] * radius, Vector3.down, out RaycastHit h, rayOrigin.y + 1, LayerMask.GetMask("CoarseTerrain"));
            float hh = h.point.y / WorldUtils.HEIGHT_STEP;
            if (hh > maxHeight)
                maxHeight = hh;
            else if (hh < minHeight)
                minHeight = hh;
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
