namespace Data.LevelGen
{
    public record TerrainTypes(TerrainType[] AllTypes)
    {
        public static TerrainTypes inst = null;
    }
}