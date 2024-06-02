using System.Collections.Generic;
using System.Linq;
using Data.Parsers;
using Utils;
using static Data.Parsers.Parsers;

namespace Data.WorldGen
{
    public record ObstaclesData(ObstacleData[][] Layers, Dictionary<string, ObstacleData> Obstacles)
    {
        public static ObstaclesData Parse(ParseStream stream)
        {
            var pp = new PropertyParserWithNamedExtra<ObstacleData>();
            var getLayers = pp.Register("layers", Chain(ParseBlock, ParseList, ParseLine, ParseList, ParseWord));
            pp.RegisterExtraParser((n, s) => ObstacleData.Parse(n, s, getLayers.GetValue().Count));

            pp.Parse(stream);

            var obstacles = new Dictionary<string, ObstacleData>();
            foreach (var obstacle in pp.ParsedExtra)
                if (!obstacles.TryAdd(obstacle.Name, obstacle))
                    throw new ParseException(stream, $"Duplicate obstacle \"{obstacle.Name}\".");

            var parsedLayers = getLayers.GetValue();
            var layers = new ObstacleData[parsedLayers.Count][];
            for (int i = 0; i < parsedLayers.Count; i++)
            {
                if (parsedLayers[i].Count == 0)
                    throw new ParseException(stream, $"Layer {i} has no obstacles specified.");
                layers[i] = new ObstacleData[parsedLayers[i].Count];
                for (int j = 0; j < parsedLayers[i].Count; j++)
                {
                    if (!obstacles.TryGetValue(parsedLayers[i][j], out var b))
                        throw new ParseException(stream, $"Obstacle \"{parsedLayers[i][j]}\" was not defined.");
                    layers[i][j] = b;
                }
            }

            return new(layers, obstacles);
        }
    }

    public record ObstacleData(string Name, ObstacleData.Type ObstacleType, int Min, int Max, float BaseProbability, BitSet32 ValidSurfaces, bool OnSlants, float[] Forces)
    {
        public enum Type { Small, Large, Fuel, Minerals }

        public static ObstacleData Parse(string name, ParseStream stream, int layerCount)
        {
            using BlockParseStream blockStream = new(stream);
            PropertyParser pp = new();
            var type = pp.Register("type", ParseType);
            var min = pp.Register("min", ParseInt, 0);
            var max = pp.Register("max", ParseInt, int.MaxValue);
            var baseProbability = pp.Register("base_probability", ParseFloat);
            var validSurfaces = pp.Register("valid_surfaces", Chain(ParseLine, ParseList, TerrainType.ParseSurface), Enumerable.Range(0, 32).ToList());
            var onSlants = pp.Register("on_slants", ParseBool, true);
            var getForces = pp.Register("forces", Chain(ParseLine, ParseList, ParseFloat));

            min.SetValidator((int value, out string err) => IsNonnegative(value, min.Name, out err));
            max.SetValidator((int value, out string err) => IsAtLeast(value, max.Name, min.GetValue(), min.Name, out err));
            validSurfaces.SetValidator(TerrainType.AreKeysDistinct);

            pp.Parse(blockStream);

            float[] forces = new float[layerCount];
            getForces.GetValue().CopyTo(forces);

            return new(name, type.GetValue(), min.GetValue(), max.GetValue(), baseProbability.GetValue(), BitSet32.FromBits(validSurfaces.GetValue()), onSlants.GetValue(), forces);
        }

        public static Type ParseType(ParseStream stream)
        {
            return ParseWord(stream) switch
            {
                "s" or "small" => Type.Small,
                "l" or "large" => Type.Large,
                "f" or "fuel" => Type.Fuel,
                "m" or "minerals" => Type.Minerals,
                { } w => throw new ParseException(stream, $"Invalid obstacle type \"{w}\".")
            };
        }
    }
}