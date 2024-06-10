using System;
using System.Linq;
using BattleSimulation.World.WorldData;
using Data.WorldGen;
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

        public float VisitConstantNode(ConstantNode node) => node.Value;
        public float VisitCompositeNode(CompositeNode node) => node.Children.Select(c => c.Accept(this)).Sum();
        public float VisitMultiplyNode(MultiplyNode node) => node.Children.Select(c => c.Accept(this)).Sum() * node.Multiplier;
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
            float minDist = float.PositiveInfinity;
            for (int radius = 0; radius < Mathf.Max(WorldUtils.WORLD_SIZE.x, WorldUtils.WORLD_SIZE.y) + 1; radius++)
            {
                int minAchievableDist = radius - 1;
                if (minAchievableDist > minDist)
                    break;
                float radiusMinDist = MinDistanceAtRadius(radius, rounded, inside, node.IsPosInside);
                minDist = Mathf.Min(minDist, radiusMinDist);
            }

            return inside ? -minDist : minDist;
        }

        float MinDistanceAtRadius(int radius, Vector2Int centerTile, bool inside, Predicate<Vector2Int> isInside)
        {
            float minDist = float.PositiveInfinity;
            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int cornerTile = centerTile + radius * WorldUtils.DIAGONAL_DIRS[direction];
                for (int i = 0; i < 2 * radius; i++)
                {
                    Vector2Int tile = cornerTile - i * WorldUtils.CARDINAL_DIRS[direction];
                    if (!WorldUtils.IsInRange(tile, WorldUtils.WORLD_SIZE) || isInside(tile) == inside)
                        continue;
                    float dist = GetSignedDistance(position_, tile);
                    minDist = Mathf.Min(minDist, dist);
                }
            }

            return minDist;
        }

        static float GetSignedDistance(Vector2 pos, Vector2Int tile)
        {
            return new Vector2(GetSignedDistance1D(pos.x, tile.x), GetSignedDistance1D(pos.y, tile.y)).magnitude;
        }

        static float GetSignedDistance1D(float pos, int tilePos)
        {
            float diff = pos - tilePos;
            return diff switch
            {
                < -0.5f => diff + 0.5f,
                > 0.5f => diff - 0.5f,
                _ => 0
            };
        }

        public float VisitHeightNode(HeightNode node)
        {
            return World.data.tiles.GetHeightAt(position_);
        }

        public float VisitFractalNoiseNode(FractalNoiseNode node) => node.Noise.EvaluateAt(position_);
    }
}