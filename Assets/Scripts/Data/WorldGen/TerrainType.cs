using System.Collections.Generic;
using System.Linq;
using Data.Parsers;
using Utils;
using static Data.Parsers.Parsers;

namespace Data.WorldGen
{
    public record TerrainType(string DisplayName, BitSet32 Surfaces, int MaxHeight, Module[] Modules, ObstaclesData Obstacles, ScattererData ScattererData, List<FractalNoiseNode> NoiseNodes)
    {
        public static TerrainType Parse(ParseStream stream)
        {
            List<FractalNoiseNode> noiseNodes = new();
            PropertyParser pp = new();
            var displayName = pp.Register("display_name", ParseWord);
            var surfaces = pp.Register("surfaces", Chain(ParseLine, ParseList, ParseSurface));
            var maxHeight = pp.Register("max_height", ParseInt);
            var modules = pp.Register("modules", Chain(ParseBlock, ParseModules));
            var obstacles = pp.Register("obstacles", Chain(ParseBlock, s => ObstaclesData.Parse(s, surfaces())));
            var scatterer = pp.Register("scatterer", Chain(ParseBlock, s => ScattererData.Parse(s, obstacles().Obstacles.Keys, noiseNodes)));

            pp.Parse(stream);

            return new(
                displayName(),
                BitSet32.FromBits(surfaces()),
                maxHeight(),
                modules(),
                obstacles(),
                scatterer(),
                noiseNodes
            );
        }

        static Module[] ParseModules(ParseStream bs)
        {
            var parser = new PropertyParserWithNamedExtra<Module[]>();
            parser.RegisterExtraParser(Module.Parse);
            parser.Parse(bs);
            return parser.ParsedExtra.SelectMany(a => a).ToArray();
        }

        public static int ParseSurface(ParseStream stream)
        {
            char c = ParseChar(stream);
            if (c is < 'A' or > 'Z')
                throw new ParseException(stream, $"\"{c}\" is not a valid surface - must be an uppercase letter.");
            return c - 'A';
        }
    }
}