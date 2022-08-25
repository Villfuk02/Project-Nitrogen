using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WFCModule
{
    public string name;
    [Header("Auto-Generation")]
    public bool enabled;
    public bool flip;
    public int rotate;
    [Header("Data")]
    public Sprite sprite;
    public float weight;
    public bool[] passable;
    public Vector3Int heightOffsets;
    public WorldUtils.TerrainTypes[] terrainTypes;
    //tile slopes
    [Header("Debug")]
    public int graphicsHeightOffset;

    public WFCModule Copy()
    {
        WFCModule m = new()
        {
            name = name,
            flip = flip,
            rotate = rotate,
            weight = weight,
            sprite = sprite,
            passable = (bool[])passable.Clone(),
            heightOffsets = new Vector3Int(heightOffsets.x, heightOffsets.y, heightOffsets.z),
            terrainTypes = (WorldUtils.TerrainTypes[])terrainTypes.Clone(),
            graphicsHeightOffset = graphicsHeightOffset,
        };
        return m;
    }
}
