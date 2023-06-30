
using UnityEngine;
using Utils;

namespace LevelGen.WFC
{
    [System.Serializable]
    public class WFCModule
    {
        public string name;
        [Header("Auto-Generation")]
        public bool enabled;
        public bool flip;
        public int rotate;
        [Header("Data")]
        public Mesh mesh;
        public float weight;
        public bool[] passable;
        public Vector3Int heightOffsets;
        public WorldUtils.TerrainType[] terrainTypes;
        public WorldUtils.Slant[] slants;
        [Header("Debug")]
        public int meshHeightOffset;

        public WFCModule Copy()
        {
            WFCModule m = new()
            {
                name = name,
                flip = flip,
                rotate = rotate,
                weight = weight,
                mesh = mesh,
                passable = (bool[])passable.Clone(),
                heightOffsets = new Vector3Int(heightOffsets.x, heightOffsets.y, heightOffsets.z),
                terrainTypes = (WorldUtils.TerrainType[])terrainTypes.Clone(),
                slants = (WorldUtils.Slant[])slants.Clone(),
                meshHeightOffset = meshHeightOffset,
            };
            return m;
        }
    }
}
