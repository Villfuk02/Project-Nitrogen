namespace LevelGen.TerrainType
{
    public record TerrainTypes(Data.LevelGen.TerrainType[] AllTypes)
    {
        public static TerrainTypes inst = null;
    }
}