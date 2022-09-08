using System.Collections.Generic;

public class WFCTile
{
    public readonly HashSet<int> heights;
    public readonly HashSet<WorldUtils.TerrainType> terrainTypes;
    public readonly HashSet<WorldUtils.Slant> slants;

    public WFCTile(bool prefill)
    {
        if (prefill)
        {
            heights = new(WorldUtils.ALL_HEIGHTS);
            terrainTypes = new(WorldUtils.ALL_TERRAIN_TYPES);
            slants = new(WorldUtils.ALL_SLANTS);
        }
        else
        {
            heights = new();
            terrainTypes = new();
            slants = new();
        }
    }

    public WFCTile(WFCTile original)
    {
        heights = new(original.heights);
        terrainTypes = new(original.terrainTypes);
        slants = new(original.slants);
    }
}
