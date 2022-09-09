using System;
using UnityEngine;

[Serializable]
public class ScattererObjectModule
{
    public string name;
    public bool enabled = true;
    public GameObject prefab;
    public bool[,] validTiles;
    public int triesPerTile;
    public float placementRadius;
    public float persistingRadius;
    public float sizeGain;
    public float radiusGain;
    public float angleSpread;
    public float minValue = float.NegativeInfinity;
    [SerializeReference, SubclassSelector] public ScattererValueModule[] valueModules;

    public float EvaluateAt(Vector2 tilePos)
    {
        float ret = 0;
        foreach (ScattererValueModule svm in valueModules)
        {
            if (svm != null)
                ret += svm.EvaluateAt(tilePos, this);
        }
        return ret;
    }

    public float GetScaled(float baseRadius, float strength, float evaluated)
    {
        float s = strength * evaluated;
        if (s < 0)
            return baseRadius / (1 - s);
        return baseRadius * (1 + s);
    }

    public ScattererObjectModule Clone()
    {
        return new()
        {
            name = name,
            enabled = enabled,
            prefab = prefab,
            triesPerTile = triesPerTile,
            placementRadius = placementRadius,
            persistingRadius = persistingRadius,
            sizeGain = sizeGain,
            radiusGain = radiusGain,
            angleSpread = angleSpread,
            valueModules = valueModules,
            minValue = minValue
        };
    }
}
