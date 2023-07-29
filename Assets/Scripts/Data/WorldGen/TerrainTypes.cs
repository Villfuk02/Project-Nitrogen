using System;
using System.Linq;

namespace Data.WorldGen
{
    public record TerrainTypes(TerrainType[] AllTypes)
    {
        public static TerrainTypes inst = null;

        public static TerrainType GetTerrainType(string name)
        {
            if (inst == null)
                throw new InvalidOperationException("Terrain types were not loaded yet.");
            TerrainType result = inst.AllTypes.FirstOrDefault(t => t.DisplayName == name);
            return result ?? throw new ArgumentException($"Terrain type {name} was not found.");
        }
    }
}