using System.Collections.Generic;
using System.Linq;
using Utils;

namespace WorldGen.WFC
{
    /// <summary>
    /// Holds information about what values are allowed at the given tile.
    /// </summary>
    public class WFCTile
    {
        public readonly HashSet<int> heights;
        public readonly HashSet<char> surfaces;
        public readonly HashSet<WorldUtils.Slant> slants;

        public WFCTile(bool prefill)
        {
            if (prefill)
            {
                heights = Enumerable.Range(0, WorldGenerator.TerrainType.MaxHeight + 1).ToHashSet();
                surfaces = new(WorldGenerator.TerrainType.Surfaces);
                slants = new(WorldUtils.ALL_SLANTS);
            }
            else
            {
                heights = new();
                surfaces = new();
                slants = new();
            }
        }
    }
}