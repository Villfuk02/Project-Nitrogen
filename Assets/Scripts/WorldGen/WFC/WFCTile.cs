using Utils;

namespace WorldGen.WFC
{
    /// <summary>
    /// Holds information about what values are allowed at the given tile.
    /// </summary>
    public class WFCTile
    {
        public BitSet32 heights;
        public BitSet32 surfaces;
        public BitSet32 slants;

        public WFCTile(bool prefill)
        {
            if (prefill)
            {
                heights = BitSet32.LowestBitsSet(WorldGenerator.TerrainType.MaxHeight + 1);
                surfaces = WorldGenerator.TerrainType.Surfaces;
                slants = WorldUtils.ALL_SLANTS;
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