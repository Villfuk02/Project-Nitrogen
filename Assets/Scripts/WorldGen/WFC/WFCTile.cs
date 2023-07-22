using System.Collections.Generic;
using System.Linq;
using Utils;

namespace WorldGen.WFC
{
    public class WFCTile
    {
        public readonly HashSet<int> heights;
        public readonly HashSet<char> surfaces;
        public readonly HashSet<WorldUtils.Slant> slants;

        public WFCTile(bool prefill)
        {
            if (prefill)
            {
                heights = Enumerable.Range(0, WFCGenerator.Terrain.MaxHeight + 1).ToHashSet();
                surfaces = new(WFCGenerator.Terrain.Surfaces);
                slants = new(WorldUtils.ALL_SLANTS);
            }
            else
            {
                heights = new();
                surfaces = new();
                slants = new();
            }
        }

        public WFCTile(WFCTile original)
        {
            heights = new(original.heights);
            surfaces = new(original.surfaces);
            slants = new(original.slants);
        }
    }
}