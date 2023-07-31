using Data.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using static Data.Parsers.Parsers;

namespace Data.WorldGen
{
    public class ScattererData
    {
        public Decoration[] decorations;
        public Predicate<Vector2Int> isPath = null;
        public Dictionary<string, Predicate<Vector2Int>> isBlocker = new();

        public static ScattererData Parse(ParseStream stream, IEnumerable<string> blockers)
        {
            ScattererData sd = new();

            foreach (string blocker in blockers)
            {
                sd.isBlocker.Add(blocker, null);
            }

            Node ParseNode(ParseStream parseStream)
            {
                string node = ParseWord(parseStream);
                SkipWhitespace(parseStream);
                switch (node)
                {
                    case "sum":
                        return CompositeNode.Parse(parseStream, ParseNode);
                    case "clamp":
                        return ClampNode.Parse(parseStream, ParseNode);
                    case "path":
                        return SDFNode.Parse(parseStream, pos => sd.isPath(pos));
                    case "blocker":
                        string blocker = ParseWord(parseStream);
                        SkipWhitespace(parseStream);
                        if (!sd.isBlocker.ContainsKey(blocker))
                            throw new ParseException(parseStream, $"Blocker \"{blocker}\" was not defined.");
                        return SDFNode.Parse(parseStream, pos => sd.isBlocker[blocker](pos));
                    case "fractal_noise":
                        return FractalNoiseNode.Parse(parseStream);
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


            pp.Parse(blockStream);

            return new(name, prefab(), triesPerTile(), placementRadius(), persistentRadius(), sizeGain(), radiusGain(), angleSpread(), valueThreshold(), value());
        }

        public float EvaluateAt(Vector2 pos) => Value.Evaluate(pos);

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

    public abstract class Node
    {
        public abstract float Evaluate(Vector2 pos);
    }

    public class CompositeNode : Node
    {
        readonly List<Node> children_;
        public CompositeNode(List<Node> children)
        {
            children_ = children;
        }

        public override float Evaluate(Vector2 pos) => children_.Select(c => c.Evaluate(pos)).Sum();
        public static CompositeNode Parse(ParseStream stream, Parse<Node> nodeFactory) => new(ParseChildren(stream, nodeFactory));
        public static List<Node> ParseChildren(ParseStream stream, Parse<Node> nodeFactory)
        {
            BlockParseStream blockStream = new(stream);
            var children = ParseList(blockStream, nodeFactory);
            return children;
        }
    }

    public class ClampNode : CompositeNode
    {
        readonly float min_;
        readonly float max_;
        public ClampNode(float min, float max, List<Node> children) : base(children)
        {
            min_ = min;
            max_ = max;
        }

        public override float Evaluate(Vector2 pos) => Mathf.Clamp(base.Evaluate(pos), min_, max_);

        public new static ClampNode Parse(ParseStream stream, Parse<Node> nodeFactory)
        {
            float min = ParseFloat(stream);
            SkipWhitespace(stream);
            float max = ParseFloat(stream);
            SkipWhitespace(stream);
            var children = ParseChildren(stream, nodeFactory);
            return new(min, max, children);
        }
    }

    public class SDFNode : Node
    {
        readonly float innerMultiplier_;
        readonly float outerMultiplier_;
        readonly Predicate<Vector2Int> isPosIn_;
        public SDFNode(float innerMultiplier, float outerMultiplier, Predicate<Vector2Int> isPosIn)
        {
            innerMultiplier_ = innerMultiplier;
            outerMultiplier_ = outerMultiplier;
            isPosIn_ = isPosIn;
        }

        public override float Evaluate(Vector2 tilePos)
        {
            float sdf = EvaluateSDF(tilePos);
            if (sdf > 0)
                return outerMultiplier_ * sdf;
            return -innerMultiplier_ * sdf;
        }

        float EvaluateSDF(Vector2 tilePos)
        {
            Vector2Int rounded = new(Mathf.RoundToInt(tilePos.x), Mathf.RoundToInt(tilePos.y));
            bool inside = isPosIn_(rounded);
            float prevMinDist = float.PositiveInfinity;
            for (int r = 0; r < Mathf.Max(WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y) + 1; r++)
            {
                float minDist = float.PositiveInfinity;
                Vector2Int boundsMin = new(Mathf.Max(rounded.x - r, 0), Mathf.Max(rounded.y - r, 0));
                Vector2Int boundsMax = new(Mathf.Min(rounded.x + r, WorldUtils.WORLD_SIZE.x - 1), Mathf.Min(rounded.y + r, WorldUtils.WORLD_SIZE.y - 1));
                if (boundsMin.x == rounded.x - r)
                {
                    for (int y = boundsMin.y; y <= boundsMax.y; y++)
                    {
                        if (isPosIn_(new(boundsMin.x, y)) == inside)
                            continue;

                        float dist = GetSignedDistance(tilePos, new(boundsMin.x, y));
                        if (dist < minDist)
                            minDist = dist;
                    }
                }
                if (boundsMax.x == rounded.x + r)
                {
                    for (int y = boundsMin.y; y <= boundsMax.y; y++)
                    {
                        if (isPosIn_(new(boundsMax.x, y)) == inside)
                            continue;

                        float dist = GetSignedDistance(tilePos, new(boundsMax.x, y));
                        if (dist < minDist)
                            minDist = dist;
                    }
                }
                if (boundsMin.y == rounded.y - r)
                {
                    for (int x = boundsMin.x; x <= boundsMax.x; x++)
                    {
                        if (isPosIn_(new(x, boundsMin.y)) == inside)
                            continue;

                        float dist = GetSignedDistance(tilePos, new(x, boundsMin.y));
                        if (dist < minDist)
                            minDist = dist;
                    }
                }
                if (boundsMax.y == rounded.y + r)
                {
                    for (int x = boundsMin.x; x <= boundsMax.x; x++)
                    {
                        if (isPosIn_(new(x, boundsMax.y)) == inside)
                            continue;

                        float dist = GetSignedDistance(tilePos, new(x, boundsMax.y));
                        if (dist < minDist)
                            minDist = dist;
                    }
                }
                if (!float.IsPositiveInfinity(minDist))
                {
                    if (!float.IsPositiveInfinity(prevMinDist))
                    {
                        if (prevMinDist < minDist)
                            minDist = prevMinDist;
                        return inside ? -minDist : minDist;
                    }
                    prevMinDist = minDist;
                }
                else if (!float.IsPositiveInfinity(prevMinDist))
                {
                    return inside ? -prevMinDist : prevMinDist;
                }
            }

            throw new();
            //return inside ? -1_000_000 : 1_000_000;
        }

        static float GetSignedDistance(Vector2 pos, Vector2Int tile)
        {
            static float CoordDiff(float pos, float target)
            {
                float diff = pos - target;
                return diff switch
                {
                    < -0.5f => diff + 0.5f,
                    > 0.5f => diff - 0.5f,
                    _ => 0
                };
            }
            return new Vector2(CoordDiff(pos.x, tile.x), CoordDiff(pos.y, tile.y)).magnitude;
        }

        public static SDFNode Parse(ParseStream stream, Predicate<Vector2Int> isPosIn)
        {
            float inner = ParseFloat(stream);
            SkipWhitespace(stream);
            float outer = ParseFloat(stream);
            return new(inner, outer, isPosIn);
        }
    }

    public class FractalNoiseNode : Node
    {
        public static readonly List<FractalNoiseNode> ALL_NODES = new();
        public readonly FractalNoise noise;

        public FractalNoiseNode(FractalNoise noise)
        {
            this.noise = noise;
            ALL_NODES.Add(this);
        }
        public override float Evaluate(Vector2 pos)
        {
            return noise.EvaluateAt(pos);
        }
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

            pp.Parse(blockStream);
            return new(new(octaves(), bias(), baseAmplitude(), amplitudeMult(), baseFrequency(), frequencyMult()));
        }
    }
}
