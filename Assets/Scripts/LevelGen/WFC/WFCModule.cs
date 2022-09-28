using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.WFC
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

        public float GetBaseHeight(float x, float y)
        {
            float baseHeight = float.NegativeInfinity;
            if (x <= 0 && y >= 0)
            {
                baseHeight = Mathf.Max(baseHeight, GetQuadrantBaseHeight(x, y - 1, 0));
            }
            if (x >= 0 && y >= 0)
            {
                baseHeight = Mathf.Max(baseHeight, GetQuadrantBaseHeight(x - 1, y - 1, 1));
            }
            if (x >= 0 && y <= 0)
            {
                baseHeight = Mathf.Max(baseHeight, GetQuadrantBaseHeight(x - 1, y, 2));
            }
            if (x <= 0 && y <= 0)
            {
                baseHeight = Mathf.Max(baseHeight, GetQuadrantBaseHeight(x, y, 3));
            }
            return baseHeight;
        }

        float GetQuadrantBaseHeight(float x, float y, int quadrant)
        {
            return quadrant switch
            {
                1 => heightOffsets.x,
                2 => heightOffsets.y,
                3 => heightOffsets.z,
                _ => 0,
            }
            + slants[quadrant] switch
            {
                WorldUtils.Slant.North => -1 - y,
                WorldUtils.Slant.East => -1 - x,
                WorldUtils.Slant.South => y,
                WorldUtils.Slant.West => x,
                _ => 0,
            };
        }
    }
}
