using System;
using System.Collections.Generic;
using Data.Parsers;
using UnityEngine;
using Utils;
using static Data.Parsers.Parsers;

namespace Data.WorldGen
{
    public class ScattererData
    {
        public Decoration[] decorations;
        public Predicate<Vector2Int> isPath = null;
        public Dictionary<string, Predicate<Vector2Int>> isObstacle = new();

        public static ScattererData Parse(ParseStream stream, IEnumerable<string> obstacles, List<FractalNoiseNode> noiseNodes)
        {
            ScattererData sd = new();

            foreach (string obstacle in obstacles)
            {
                sd.isObstacle.Add(obstacle, null);
            }

            Node ParseNode(ParseStream parseStream)
            {
                string node = ParseWord(parseStream);
                SkipWhitespace(parseStream);
                switch (node)
                {
                    case "const":
                        return ConstantNode.Parse(parseStream);
                    case "sum":
                        return CompositeNode.Parse(parseStream, ParseNode);
                    case "mult":
                        return MultiplyNode.Parse(parseStream, ParseNode);
                    case "clamp":
                        return ClampNode.Parse(parseStream, ParseNode);
                    case "path":
                        return SDFNode.Parse(parseStream, pos => sd.isPath(pos));
                    case "obstacle":
                        string obstacle = ParseWord(parseStream);
                        SkipWhitespace(parseStream);
                        if (!sd.isObstacle.ContainsKey(obstacle))
                            throw new ParseException(parseStream, $"Obstacle \"{obstacle}\" was not defined.");
                        return SDFNode.Parse(parseStream, pos => sd.isObstacle[obstacle](pos));
                    case "height":
                        return new HeightNode();
                    case "fractal_noise":
                        var n = FractalNoiseNode.Parse(parseStream);
                        noiseNodes.Add(n);
                        return n;
                    default:
                        throw new ParseException(parseStream, $"Invalid scatterer node \"{node}\".");
                }
            }

            var pp = new PropertyParserWithNamedExtra<Decoration>();
            pp.RegisterExtraParser((n, s) => Decoration.Parse(n, s, ParseNode));

            pp.Parse(stream);

            sd.decorations = pp.ParsedExtra.ToArray();
            return sd;
        }
    }

    public record Decoration(string Name, GameObject Prefab, int TriesPerTile, float PlacementRadius, float PersistentRadius, float SizeGain, float RadiusGain, float AngleSpread, float ValueThreshold, Node Value)
    {
        public static Decoration Parse(string name, ParseStream stream, Parse<Node> nodeFactory)
        {
            using var blockStream = new BlockParseStream(stream);
            PropertyParser pp = new();
            var prefab = pp.Register("prefab", ParseAndLoadResource<GameObject>);
            var triesPerTile = pp.Register("tries_per_tile", ParseInt);
            var placementRadius = pp.Register("placement_radius", ParseFloat);
            var persistentRadius = pp.Register("persistent_radius", ParseFloat);
            var sizeGain = pp.Register("size_gain", ParseFloat);
            var radiusGain = pp.Register("radius_gain", ParseFloat);
            var angleSpread = pp.Register("angle_spread", ParseFloat);
            var valueThreshold = pp.Register("value_threshold", ParseFloat);
            var value = pp.Register("value", s => CompositeNode.Parse(s, nodeFactory));

            triesPerTile.SetValidator((int v, out string err) => IsPositive(v, triesPerTile.Name, out err));
            placementRadius.SetValidator((float v, out string err) => IsNonnegative(v, placementRadius.Name, out err));
            persistentRadius.SetValidator((float v, out string err) => IsNonnegative(v, persistentRadius.Name, out err));
            angleSpread.SetValidator((float v, out string err) => IsNonnegative(v, angleSpread.Name, out err));

            pp.Parse(blockStream);

            return new(name, prefab.GetValue(), triesPerTile.GetValue(), placementRadius.GetValue(),
                persistentRadius.GetValue(), sizeGain.GetValue(), radiusGain.GetValue(), angleSpread.GetValue(), valueThreshold.GetValue(), value.GetValue());
        }

        static float GetScaled(float baseRadius, float strength, float evaluated)
        {
            if (baseRadius == 0)
                return 0;
            float s = strength * evaluated;
            if (s < 0)
                return baseRadius / (1 - s);
            return baseRadius * (1 + s);
        }

        public float GetScale(float evaluated) => GetScaled(1, SizeGain, evaluated);
        public float GetPlacementSize(float evaluated) => GetScaled(PlacementRadius, RadiusGain, evaluated);
        public float GetColliderSize(float evaluated) => GetScaled(PersistentRadius, RadiusGain, evaluated);
    }

    public interface IDecorationNodeVisitor<out T>
    {
        public T VisitConstantNode(ConstantNode node);
        public T VisitCompositeNode(CompositeNode node);
        public T VisitMultiplyNode(MultiplyNode node);
        public T VisitClampNode(ClampNode node);
        public T VisitSDFNode(SDFNode node);
        public T VisitHeightNode(HeightNode node);
        public T VisitFractalNoiseNode(FractalNoiseNode node);
    }

    public abstract record Node
    {
        public abstract T Accept<T>(IDecorationNodeVisitor<T> visitor);
    }

    public record ConstantNode(float Value) : Node
    {
        public static ConstantNode Parse(ParseStream stream)
        {
            float value = ParseFloat(stream);
            return new(value);
        }

        public override T Accept<T>(IDecorationNodeVisitor<T> visitor) => visitor.VisitConstantNode(this);
    }

    public record CompositeNode(List<Node> Children) : Node
    {
        public static CompositeNode Parse(ParseStream stream, Parse<Node> nodeFactory) => new(ParseChildren(stream, nodeFactory));

        public static List<Node> ParseChildren(ParseStream stream, Parse<Node> nodeFactory)
        {
            BlockParseStream blockStream = new(stream);
            var children = ParseList(blockStream, nodeFactory);
            return children;
        }

        public override T Accept<T>(IDecorationNodeVisitor<T> visitor) => visitor.VisitCompositeNode(this);
    }

    public record MultiplyNode(float Multiplier, List<Node> Children) : CompositeNode(Children)
    {
        public new static MultiplyNode Parse(ParseStream stream, Parse<Node> nodeFactory)
        {
            float mult = ParseFloat(stream);
            SkipWhitespace(stream);
            var children = ParseChildren(stream, nodeFactory);
            return new(mult, children);
        }

        public override T Accept<T>(IDecorationNodeVisitor<T> visitor) => visitor.VisitMultiplyNode(this);
    }

    public record ClampNode(float Min, float Max, List<Node> Children) : CompositeNode(Children)
    {
        public new static ClampNode Parse(ParseStream stream, Parse<Node> nodeFactory)
        {
            float min = ParseFloat(stream);
            SkipWhitespace(stream);
            float max = ParseFloat(stream);
            SkipWhitespace(stream);
            var children = ParseChildren(stream, nodeFactory);
            return new(min, max, children);
        }

        public override T Accept<T>(IDecorationNodeVisitor<T> visitor) => visitor.VisitClampNode(this);
    }

    public record SDFNode(float InnerMultiplier, float OuterMultiplier, Predicate<Vector2Int> IsPosInside) : Node
    {
        public static SDFNode Parse(ParseStream stream, Predicate<Vector2Int> isPosIn)
        {
            float inner = ParseFloat(stream);
            SkipWhitespace(stream);
            float outer = ParseFloat(stream);
            return new(inner, outer, isPosIn);
        }

        public override T Accept<T>(IDecorationNodeVisitor<T> visitor) => visitor.VisitSDFNode(this);
    }

    public record HeightNode : Node
    {
        public override T Accept<T>(IDecorationNodeVisitor<T> visitor) => visitor.VisitHeightNode(this);
    }

    public record FractalNoiseNode(FractalNoise Noise) : Node
    {
        public static FractalNoiseNode Parse(ParseStream stream)
        {
            using BlockParseStream blockStream = new(stream);
            PropertyParser pp = new();
            var octaves = pp.Register("octaves", ParseInt);
            var bias = pp.Register("bias", ParseFloat);
            var baseAmplitude = pp.Register("base_amplitude", ParseFloat);
            var amplitudeMult = pp.Register("amplitude_mult", ParseFloat);
            var baseFrequency = pp.Register("base_frequency", ParseFloat);
            var frequencyMult = pp.Register("frequency_mult", ParseFloat);

            octaves.SetValidator((int value, out string err) => IsPositive(value, octaves.Name, out err));
            amplitudeMult.SetValidator((float value, out string err) => IsPositive(value, amplitudeMult.Name, out err));
            frequencyMult.SetValidator((float value, out string err) => IsPositive(value, frequencyMult.Name, out err));

            pp.Parse(blockStream);
            return new(new FractalNoise(octaves.GetValue(), bias.GetValue(), baseAmplitude.GetValue(), amplitudeMult.GetValue(), baseFrequency.GetValue(), frequencyMult.GetValue()));
        }

        public override T Accept<T>(IDecorationNodeVisitor<T> visitor) => visitor.VisitFractalNoiseNode(this);
    }
}