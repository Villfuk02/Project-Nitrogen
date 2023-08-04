using Data.WorldGen;
using System.Linq;
using UnityEngine;
using Utils;

namespace WorldGen.Decorations
{
    public class DecorationEvaluator : IDecorationNodeVisitor<float>
    {
        readonly Vector2 position_;

        public DecorationEvaluator(Vector2 position)
        {
            position_ = position;
        }

        public float Evaluate(Decoration decoration) => decoration.Value.Accept(this);

        public float VisitCompositeNode(CompositeNode node) => node.Children.Select(c => c.Accept(this)).Sum();
        public float VisitClampNode(ClampNode node) => Mathf.Clamp(VisitCompositeNode(node), node.Min, node.Max);
        public float VisitSDFNode(SDFNode node)
        {
            float sdf = EvaluateSDF(node);
            if (sdf > 0)
                return node.OuterMultiplier * sdf;
            return -node.InnerMultiplier * sdf;
        }
        float EvaluateSDF(SDFNode node)
        {
            Vector2Int rounded = position_.Round();
            bool inside = node.IsPosInside(rounded);
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
                        if (node.IsPosInside(new(boundsMin.x, y)) == inside)
                            continue;

                        float dist = GetSignedDistance(position_, new(boundsMin.x, y));
                        if (dist < minDist)
                            minDist = dist;
                    }
                }
                if (boundsMax.x == rounded.x + r)
                {
                    for (int y = boundsMin.y; y <= boundsMax.y; y++)
                    {
                        if (node.IsPosInside(new(boundsMax.x, y)) == inside)
                            continue;

                        float dist = GetSignedDistance(position_, new(boundsMax.x, y));
                        if (dist < minDist)
                            minDist = dist;
                    }
                }
                if (boundsMin.y == rounded.y - r)
                {
                    for (int x = boundsMin.x; x <= boundsMax.x; x++)
                    {
                        if (node.IsPosInside(new(x, boundsMin.y)) == inside)
                            continue;

                        float dist = GetSignedDistance(position_, new(x, boundsMin.y));
                        if (dist < minDist)
                            minDist = dist;
                    }
                }
                if (boundsMax.y == rounded.y + r)
                {
                    for (int x = boundsMin.x; x <= boundsMax.x; x++)
                    {
                        if (node.IsPosInside(new(x, boundsMax.y)) == inside)
                            continue;

                        float dist = GetSignedDistance(position_, new(x, boundsMax.y));
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

        public float VisitFractalNoiseNode(FractalNoiseNode node) => node.Noise.EvaluateAt(position_);
    }
}
