using Data.Parsers;
using System.Collections.Generic;
using System.Linq;
using static Data.Parsers.Parsers;

namespace Data.WorldGen
{
    public record BlockersData(BlockerData[][] Layers, BlockerData[] Fillers, Dictionary<string, BlockerData> Blockers)
    {
        public static BlockersData Parse(ParseStream stream, IEnumerable<char> allSurfaces)
        {
            var pp = new PropertyParserWithNamedExtra<BlockerData>();
            var getLayers = pp.Register("layers", Chain(ParseBlock, ParseList, ParseLine, ParseList, ParseWord));
            var getFillers = pp.Register("fillers", Chain(ParseLine, ParseList, ParseWord));
            pp.RegisterExtraParser((n, s) => BlockerData.Parse(n, s, getLayers().Count, allSurfaces.ToArray()));

            pp.Parse(stream);

            var blockers = new Dictionary<string, BlockerData>();
            foreach (var blocker in pp.ParsedExtra)
            {
                if (blockers.ContainsKey(blocker.Name))
                    throw new ParseException(stream, $"Duplicate blocker \"{blocker.Name}\".");
                blockers[blocker.Name] = blocker;
            }

            var parsedLayers = getLayers();
            var layers = new BlockerData[parsedLayers.Count][];
            for (int i = 0; i < parsedLayers.Count; i++)
            {
                if (parsedLayers[i].Count == 0)
                    throw new ParseException(stream, $"Layer {i} has no blockers specified.");
                layers[i] = new BlockerData[parsedLayers[i].Count];
                for (int j = 0; j < parsedLayers[i].Count; j++)
                {
                    if (!blockers.TryGetValue(parsedLayers[i][j], out var b))
                        throw new ParseException(stream, $"Blocker \"{parsedLayers[i][j]}\" was not defined.");
                    layers[i][j] = b;
                }
            }

            var parsedFillers = getFillers();
            if (parsedFillers.Count == 0)
                throw new ParseException(stream, "No fillers specified.");
            var fillers = new BlockerData[parsedFillers.Count];
            for (int j = 0; j < parsedFillers.Count; j++)
            {
                if (!blockers.TryGetValue(parsedFillers[j], out var b))
                    throw new ParseException(stream, $"Blocker \"{parsedFillers[j]}\" was not defined.");
                fillers[j] = b;
            }

            return new(layers, fillers, blockers);
        }
    }

    public record BlockerData(string Name, BlockerData.Type BlockerType, int Min, int Max, float BaseProbability, char[] ValidSurfaces, bool OnSlants, float[] Forces)
    {
        public enum Type { Small, Large, Fuel, Minerals }
        public static BlockerData Parse(string name, ParseStream stream, int layerCount, char[] allSurfaces)
        {
            using BlockParseStream blockStream = new(stream);
            PropertyParser pp = new();
            var type = pp.Register("type", ParseType);
            var min = pp.Register("min", ParseInt, 0);
            var max = pp.Register("max", ParseInt, int.MaxValue);
            var baseProbability = pp.Register("base_probability", ParseFloat);
            var validSurfaces = pp.Register("valid_surfaces", s => Chain(ParseLine, ParseList, ParseChar)(s).ToArray(), allSurfaces);
            var onSlants = pp.Register("on_slants", ParseBool, true);
            var getForces = pp.Register("forces", Chain(ParseLine, ParseList, ParseFloat));

            pp.Parse(blockStream);

            float[] forces = new float[layerCount];
            getForces().CopyTo(forces);

            return new(name, type(), min(), max(), baseProbability(), validSurfaces(), onSlants(), forces);
        }

        public static Type ParseType(ParseStream stream)
        {
            return ParseWord(stream) switch
            {
                "s" or "small" => Type.Small,
                "l" or "large" => Type.Large,
                "f" or "fuel" => Type.Fuel,
                "m" or "minerals" => Type.Minerals,
                { } w => throw new ParseException(stream, $"Invalid blocker type \"{w}\".")
            };
        }
    }
}
