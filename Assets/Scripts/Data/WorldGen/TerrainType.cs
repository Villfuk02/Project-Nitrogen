using Data.Parsers;
using System.Collections.Generic;
using System.Linq;
using static Data.Parsers.Parsers;

namespace Data.WorldGen
{
    public record TerrainType(string DisplayName, char[] Surfaces, int MaxHeight, Module[] Modules, BlockersData Blockers, ScattererData ScattererData, List<FractalNoiseNode> NoiseNodes)
    {
        public static TerrainType Parse(ParseStream stream)
        {
            List<FractalNoiseNode> noiseNodes = new();
            PropertyParser pp = new();
            var displayName = pp.Register("display_name", ParseWord);
            var surfaces = pp.Register("surfaces", Chain(ParseLine, ParseList, ParseChar));
            var maxHeight = pp.Register("max_height", ParseInt);
            var modules = pp.Register("modules", Chain(ParseBlock, ParseModules));
            var blockers = pp.Register("blockers", Chain(ParseBlock, s => BlockersData.Parse(s, surfaces())));
            var scatterer = pp.Register("scatterer", Chain(ParseBlock, s => ScattererData.Parse(s, blockers().Blockers.Keys, noiseNodes)));

            pp.Parse(stream);

            return new(
                displayName(),
                surfaces().ToArray(),
                maxHeight(),
                modules(),
                blockers(),
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
    }
}
