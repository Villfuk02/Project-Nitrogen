using Data.Parsers;
using System.Linq;
using static Data.Parsers.Parsers;

namespace Data.LevelGen
{
    public record TerrainType(string DisplayName, char[] Surfaces, int MaxHeight, Module[] Modules, BlockersData Blockers, ScattererData ScattererData)
    {
        public static TerrainType Parse(ParseStream stream)
        {
            PropertyParser pp = new();
            var displayName = pp.Register("display_name", ParseWord);
            var surfaces = pp.Register("surfaces", Chain(ParseLine, ParseList, ParseChar));
            var maxHeight = pp.Register("max_height", ParseInt);
            var modules = pp.Register("modules", Chain(ParseBlock, ParseModules));
            var blockers = pp.Register("blockers", Chain(ParseBlock, s => BlockersData.Parse(s, surfaces())));
            var scatterer = pp.Register("scatterer", Chain(ParseBlock, s => ScattererData.Parse(s, blockers().Blockers.Keys)));

            pp.Parse(stream);

            return new(
                displayName(),
                surfaces().ToArray(),
                maxHeight(),
                modules(),
                blockers(),
                scatterer()
            );
        }

        static Module[] ParseModules(ParseStream bs)
        {
            PropertyParserWithNamedExtra<Module[]> parser = new();
            parser.RegisterExtraParser(Module.Parse);
            parser.Parse(bs);
            return parser.ParsedExtra.SelectMany(a => a).ToArray();
        }
    }
}
