using System.Collections.Generic;
using System.Linq;
using Data.Parsers;
using UnityEngine;
using Utils;
using static Data.Parsers.Parsers;

namespace Data.WorldGen
{
    public record Module(string Name, float Weight, bool Flipped, int Rotated, int HeightOffset, Mesh CollisionMesh, ModuleShape Shape)
    {
        public static Module[] Parse(string name, ParseStream stream)
        {
            using BlockParseStream blockStream = new(stream);
            PropertyParser pp = new();
            var weight = pp.Register("weight", ParseFloat);
            var variants = pp.Register("variants", Chain(ParseLine, ParseVariants), (false, 0));
            var heightOffset = pp.Register("height_offset", ParseInt, 0);
            var collisionPath = pp.Register("collision", ParseWord);
            var shape = pp.Register("shape", Chain(ParseBlock, ModuleShape.Parse));

            weight.SetValidator((float v, out string err) => IsPositive(v, weight.Name, out err));

            pp.Parse(blockStream);

            (bool flipped, int rotated) = variants.GetValue();
            var mesh = Resources.Load<Mesh>(collisionPath.GetValue()) ?? throw new ParseException(blockStream, $"Could not load mesh at \"{collisionPath.GetValue()}\"");

            Module settings = new(name, weight.GetValue(), flipped, rotated, heightOffset.GetValue(), mesh, shape.GetValue());

            return settings.MakeVariants();
        }

        public static (bool flipped, int rotated) ParseVariants(ParseStream stream)
        {
            bool flipped = false;
            int rotated = 0;
            while (stream.TryRead(out char c))
            {
                if (char.IsWhiteSpace(c))
                    continue;
                switch (c)
                {
                    case 'f':
                    case 'F':
                        flipped = true;
                        break;
                    case '0':
                        rotated = 0;
                        break;
                    case '2':
                        rotated = 2;
                        break;
                    case '4':
                        rotated = 4;
                        break;
                    default:
                        throw new ParseException(stream, $"Invalid variant flag \'{c}\'.");
                }
            }

            return (flipped, rotated);
        }

        Module[] MakeVariants()
        {
            IEnumerable<Module> m = new[] { this };
            if (Flipped)
            {
                m = m.SelectMany(n =>
                    new[]
                    {
                        n with { Flipped = false, Weight = n.Weight / 2 },
                        n with { Name = n.Name + " F", Weight = n.Weight / 2, Shape = n.Shape.Flipped() }
                    });
            }

            m = Rotated switch
            {
                2 => m.SelectMany(n => new[]
                {
                    n with { Rotated = 0, Weight = n.Weight / 2 },
                    n with { Name = n.Name + " 2", Weight = n.Weight / 2, Shape = n.Shape.Rotated(2) }
                }),
                4 => m.SelectMany(n => new[]
                {
                    n with { Rotated = 0, Weight = n.Weight / 4 },
                    n with { Rotated = 1, Name = n.Name + " 1", Weight = n.Weight / 4, Shape = n.Shape.Rotated(1) },
                    n with { Rotated = 2, Name = n.Name + " 2", Weight = n.Weight / 4, Shape = n.Shape.Rotated(2) },
                    n with { Rotated = 3, Name = n.Name + " 3", Weight = n.Weight / 4, Shape = n.Shape.Rotated(3) }
                }),
                _ => m
            };

            return m.ToArray();
        }
    }

    public record ModuleShape(DiagonalDirs<int> Surfaces, DiagonalDirs<int> Heights, DiagonalDirs<WorldUtils.Slant> Slants, CardinalDirs<int> Edges)
    {
        public static ModuleShape Parse(ParseStream stream)
        {
            DiagonalDirs<int> surfaces;
            DiagonalDirs<int> heights;
            DiagonalDirs<WorldUtils.Slant> slants;
            CardinalDirs<int> edges;

            SkipWhitespace(stream);
            surfaces.NW = TerrainType.ParseSurface(stream);
            heights.NW = ParseHeight(stream.Read());
            slants.NW = ParseSlant(stream.Read());
            SkipWhitespace(stream);
            edges.N = TerrainType.ParseEdge(stream);
            SkipWhitespace(stream);
            surfaces.NE = TerrainType.ParseSurface(stream);
            heights.NE = ParseHeight(stream.Read());
            slants.NE = ParseSlant(stream.Read());
            SkipWhitespace(stream);
            edges.W = TerrainType.ParseEdge(stream);
            SkipWhitespace(stream);
            edges.E = TerrainType.ParseEdge(stream);
            SkipWhitespace(stream);
            surfaces.SW = TerrainType.ParseSurface(stream);
            heights.SW = ParseHeight(stream.Read());
            slants.SW = ParseSlant(stream.Read());
            SkipWhitespace(stream);
            edges.S = TerrainType.ParseEdge(stream);
            SkipWhitespace(stream);
            surfaces.SE = TerrainType.ParseSurface(stream);
            heights.SE = ParseHeight(stream.Read());
            slants.SE = ParseSlant(stream.Read());

            return new(surfaces, heights, slants, edges);

            int ParseHeight(char c) => c is >= '0' and <= '9'
                ? c - '0'
                : throw new ParseException(stream, $"Invalid height \'{c}\'. It must be a digit from 0 to 9.");

            WorldUtils.Slant ParseSlant(char c) => c switch
            {
                'x' => WorldUtils.Slant.None,
                '^' => WorldUtils.Slant.North,
                '>' => WorldUtils.Slant.East,
                'v' => WorldUtils.Slant.South,
                '<' => WorldUtils.Slant.West,
                _ => throw new ParseException(stream, $"Invalid slant \'{c}\'.")
            };
        }

        public ModuleShape Rotated(int steps) => new(Surfaces.Rotated(steps), Heights.Rotated(steps), Slants.Rotated(steps, WorldUtils.RotateSlant), Edges.Rotated(steps));
        public ModuleShape Flipped() => new(Surfaces.Flipped(), Heights.Flipped(), Slants.Flipped(WorldUtils.FlipSlant), Edges.Flipped());
    }
}