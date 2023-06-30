using Data.Parsers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using static Data.Parsers.Parsers;

namespace Data.LevelGen
{
    // TODO: Finish Models
    public record Module(string Name, float Weight, bool Flipped, int Rotated, int HeightOffset, Mesh Collision, ModuleShape Shape, string[] Models)
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
            var getModels = pp.Register("models", Chain(ParseLine, ParseList, ParseWord), null);

            pp.Parse(blockStream);

            (bool flipped, int rotated) = variants();
            var mesh = Resources.Load<Mesh>(collisionPath()) ?? throw new ParseException(blockStream, $"Could not load prefab at \"{collisionPath()}\"");
            var models = getModels()?.ToArray() ?? new[] { collisionPath() };

            Module settings = new(name, weight(), flipped, rotated, heightOffset(), mesh, shape(), models);

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
                        flipped = true;
                        break;
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
                new[]{
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

    public record ModuleShape(DiagonalDirs<char> Surfaces, DiagonalDirs<int> Heights, DiagonalDirs<WorldUtils.Slant> Slants, OrthogonalDirs<bool> Passable)
    {
        public static ModuleShape Parse(ParseStream stream)
        {
            DiagonalDirs<char> surfaces;
            DiagonalDirs<int> heights;
            DiagonalDirs<WorldUtils.Slant> slants;
            OrthogonalDirs<bool> passable;

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

            bool ParsePassable(char c) => c switch
            {
                'o' => true,
                'x' => false,
                _ => throw new ParseException(stream, $"Invalid passable \'{c}\'.")
            };

            SkipWhitespace(stream);
            surfaces.NW = stream.Read();
            heights.NW = ParseHeight(stream.Read());
            slants.NW = ParseSlant(stream.Read());
            SkipWhitespace(stream);
            passable.N = ParsePassable(stream.Read());
            SkipWhitespace(stream);
            surfaces.NE = stream.Read();
            heights.NE = ParseHeight(stream.Read());
            slants.NE = ParseSlant(stream.Read());
            SkipWhitespace(stream);
            passable.W = ParsePassable(stream.Read());
            SkipWhitespace(stream);
            passable.E = ParsePassable(stream.Read());
            SkipWhitespace(stream);
            surfaces.SW = stream.Read();
            heights.SW = ParseHeight(stream.Read());
            slants.SW = ParseSlant(stream.Read());
            SkipWhitespace(stream);
            passable.S = ParsePassable(stream.Read());
            SkipWhitespace(stream);
            surfaces.SE = stream.Read();
            heights.SE = ParseHeight(stream.Read());
            slants.SE = ParseSlant(stream.Read());

            return new(surfaces, heights, slants, passable);
        }

        public ModuleShape Rotated(int steps) => new(Surfaces.Rotated(steps), Heights.Rotated(steps), Slants.Rotated(steps, WorldUtils.RotateSlant), Passable.Rotated(steps));
        public ModuleShape Flipped() => new(Surfaces.Flipped(), Heights.Flipped(), Slants.Flipped(WorldUtils.FlipSlant), Passable.Flipped());
    }
}
