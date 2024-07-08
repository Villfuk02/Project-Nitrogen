using System.Collections.Generic;
using System.Linq;
using Data.Parsers;
using Utils;
using static Data.Parsers.Parsers;

namespace Data.WorldGen
{
    public record ObstaclesData(ObstacleData[][] Phases, Dictionary<string, ObstacleData> Obstacles)
    {
        public static ObstaclesData Parse(ParseStream stream)
        {
            var pp = new PropertyParserWithNamedExtra<ObstacleData>();
            var getPhases = pp.Register("phases", Chain(ParseBlock, ParseList, ParseLine, ParseList, ParseWord));
            pp.RegisterExtraParser((n, s) => ObstacleData.Parse(n, s, getPhases.GetValue().Count));

            pp.Parse(stream);

            var obstacles = new Dictionary<string, ObstacleData>();
            foreach (var obstacle in pp.ParsedExtra)
                if (!obstacles.TryAdd(obstacle.Name, obstacle))
                    throw new ParseException(stream, $"Duplicate obstacle \"{obstacle.Name}\".");

            var parsedPhase = getPhases.GetValue();
            var phases = new ObstacleData[parsedPhase.Count][];
            for (int i = 0; i < parsedPhase.Count; i++)
            {
                if (parsedPhase[i].Count == 0)
                    throw new ParseException(stream, $"Phase {i} has no obstacles specified.");
                phases[i] = new ObstacleData[parsedPhase[i].Count];
                for (int j = 0; j < parsedPhase[i].Count; j++)
                {
                    if (!obstacles.TryGetValue(parsedPhase[i][j], out var b))
                        throw new ParseException(stream, $"Obstacle \"{parsedPhase[i][j]}\" was not defined.");
                    phases[i][j] = b;
                }
            }

            return new(phases, obstacles);
        }
    }

    public record ObstacleData(string Name, ObstacleData.Type ObstacleType, int Min, int Max, float BaseProbability, BitSet32 ValidSurfaces, bool OnSlants, float[] Affinities)
    {
        public enum Type { Small, Large, Fuel, Minerals }

        public static ObstacleData Parse(string name, ParseStream stream, int phaseCount)
        {
            using BlockParseStream blockStream = new(stream);
            PropertyParser pp = new();
            var type = pp.Register("type", ParseType);
            var min = pp.Register("min", ParseInt, 0);
            var max = pp.Register("max", ParseInt, int.MaxValue);
            var baseProbability = pp.Register("base_probability", ParseFloat);
            var validSurfaces = pp.Register("valid_surfaces", Chain(ParseLine, ParseList, TerrainType.ParseSurface), Enumerable.Range(0, 32).ToList());
            var onSlants = pp.Register("on_slants", ParseBool, true);
            var getAffinities = pp.Register("affinities", Chain(ParseLine, ParseList, ParseFloat));

            min.SetValidator((int value, out string err) => IsNonnegative(value, min.Name, out err));
            max.SetValidator((int value, out string err) => IsAtLeast(value, max.Name, min.GetValue(), min.Name, out err));
            validSurfaces.SetValidator(TerrainType.AreKeysDistinct);

            pp.Parse(blockStream);

            float[] affinities = new float[phaseCount];
            getAffinities.GetValue().CopyTo(affinities);

            return new(name, type.GetValue(), min.GetValue(), max.GetValue(), baseProbability.GetValue(), BitSet32.FromBits(validSurfaces.GetValue()), onSlants.GetValue(), affinities);
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