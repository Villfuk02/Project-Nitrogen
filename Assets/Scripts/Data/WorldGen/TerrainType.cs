using System.Collections.Generic;
using System.Linq;
using Data.Parsers;
using Utils;
using static Data.Parsers.Parsers;

namespace Data.WorldGen
{
    public record TerrainType(string DisplayName, BitSet32 Surfaces, BitSet32 FreeEdges, BitSet32 BlockedEdges, int MaxHeight, Module[] Modules, ObstaclesData Obstacles, ScattererData ScattererData, List<FractalNoiseNode> NoiseNodes)
    {
        public static TerrainType Parse(ParseStream stream)
        {
            List<FractalNoiseNode> noiseNodes = new();
            PropertyParser pp = new();
            var displayName = pp.Register("display_name", ParseWord);
            var surfaces = pp.Register("surfaces", Chain(ParseLine, ParseList, ParseSurface));
            var freeEdges = pp.Register("free_edges", Chain(ParseLine, ParseList, ParseEdge));
            var blockedEdges = pp.Register("blocked_edges", Chain(ParseLine, ParseList, ParseEdge));
            var maxHeight = pp.Register("max_height", ParseInt);
            var modules = pp.Register("modules", Chain(ParseBlock, ParseModules));
            var obstacles = pp.Register("obstacles", Chain(ParseBlock, ObstaclesData.Parse));
            var scatterer = pp.Register("scatterer", Chain(ParseBlock, s => ScattererData.Parse(s, obstacles.GetValue().Obstacles.Keys, noiseNodes)));

            surfaces.SetValidator(AreKeysDistinct);
            freeEdges.SetValidator((List<int> value, out string err) => AreEdgesValid(value, blockedEdges.GetValue(), out err));
            blockedEdges.SetValidator((List<int> value, out string err) => AreEdgesValid(value, freeEdges.GetValue(), out err));
            maxHeight.SetValidator((int value, out string err) => IsInRange(value, maxHeight.Name, 0, 9, out err));

            pp.Parse(stream);

            return new(
                displayName.GetValue(),
                BitSet32.FromBits(surfaces.GetValue()),
                BitSet32.FromBits(freeEdges.GetValue()),
                BitSet32.FromBits(blockedEdges.GetValue()),
                maxHeight.GetValue(),
                modules.GetValue(),
                obstacles.GetValue(),
                scatterer.GetValue(),
                noiseNodes
            );
        }

        public static bool AreKeysDistinct(IReadOnlyCollection<int> bits, out string err)
        {
            err = "";
            if (bits.AllDistinct())
                return true;
            var duplicates = bits.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
            err = $"Duplicate entries [{string.Join(", ", duplicates.Select(b => (char)('A' + b)))}].";
            return false;
        }

        static bool AreEdgesValid(IReadOnlyCollection<int> bits, IEnumerable<int> other, out string err)
        {
            if (!AreKeysDistinct(bits, out err))
                return false;
            var intersection = bits.Intersect(other).ToList();
            if (intersection.Count == 0)
                return true;
            err = $"Edge types [{string.Join(", ", intersection.Select(b => (char)('A' + b)))}] are both free and blocked. This is not allowed.";
            return false;
        }

        static Module[] ParseModules(ParseStream bs)
        {
            var parser = new PropertyParserWithNamedExtra<Module[]>();
            parser.RegisterExtraParser(Module.Parse);
            parser.Parse(bs);
            return parser.ParsedExtra.SelectMany(a => a).ToArray();
        }

        static int ParseKey(ParseStream stream, string entryType)
        {
            char c = ParseChar(stream);
            if (c is < 'A' or > 'Z')
                throw new ParseException(stream, $"\"{c}\" is not a valid {entryType} type - it must be an uppercase letter.");
            return c - 'A';
        }

        public static int ParseSurface(ParseStream stream) => ParseKey(stream, "surface");
        public static int ParseEdge(ParseStream stream) => ParseKey(stream, "edge");
    }
}