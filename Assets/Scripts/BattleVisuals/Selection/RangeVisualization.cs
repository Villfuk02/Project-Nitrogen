using System.Collections.Generic;
using System.Linq;
using BattleSimulation.World.WorldData;
using BattleVisuals.Selection.Highlightable;
using UnityEngine;
using Utils;

namespace BattleVisuals.Selection
{
    public class RangeVisualization : MonoBehaviour
    {
        static readonly TextureFormat TextureFormat = TextureFormat.R8;
        static readonly int HighlightMap = Shader.PropertyToID("_HighlightMap");
        static readonly int WorldSize = Shader.PropertyToID("_WorldSize");
        static readonly int Offset = Shader.PropertyToID("_Offset");
        [Header("References")]
        [SerializeField] HighlightController highlightController;
        [SerializeField] Material terrainMaterial;
        [Header("Settings")]
        [SerializeField] int pixelsPerUnit;
        [SerializeField] int levels;
        [SerializeField] float areaSamplesPerFrameMultiplier;
        [Header("Runtime variables")]
        QuadTree<(HighlightType h, bool done)> rangeVisuals_;
        readonly PriorityQueue<QuadTree<(HighlightType h, bool done)>, float> rangeVisualQueue_ = new();
        int textureSize_;
        Vector2 offset_;
        Texture2D texture_;

        void Awake()
        {
            InitMaterial();
        }

        public void UpdateVisuals(Vector2 priorityTilePosition)
        {
            int steps = Mathf.CeilToInt(highlightController.highlightProvider.AreaSamplesPerFrame * areaSamplesPerFrameMultiplier);
            for (int i = 0; i < steps; i++)
            {
                if (!rangeVisualQueue_.TryDequeue(out var node, out float _))
                    break;
                TryExpandNode(node, priorityTilePosition);
            }

            texture_.Apply(false);
        }

        public void ResetVisuals()
        {
            rangeVisuals_ = new(Vector2Int.zero, 0, CalculateRangeVisualAt, null);
            rangeVisualQueue_.Clear();
            rangeVisualQueue_.Enqueue(rangeVisuals_, 0);
            PaintNode(rangeVisuals_.pos, rangeVisuals_.depth, rangeVisuals_.value.h);
            texture_.Apply(false);
        }

        public void ClearVisuals()
        {
            rangeVisuals_ = null;
            rangeVisualQueue_.Clear();
            PaintNode(Vector2Int.zero, 0, HighlightType.Clear);
            texture_.Apply(false);
        }

        Vector2 PixelToTilePos(Vector2Int pixelPos, int size)
        {
            return (pixelPos * size + (size - 1) * 0.5f * Vector2.one) / pixelsPerUnit - offset_ + WorldUtils.WORLD_CENTER;
        }

        (HighlightType h, bool done) CalculateRangeVisualAt(Vector2Int pixel, int depth)
        {
            int size = textureSize_ >> depth;
            (var h, bool done) = CalculateRangeVisualAt(PixelToTilePos(pixel, size), size / (float)pixelsPerUnit);
            return (h, done || size == 1);
        }

        (HighlightType h, bool done) CalculateRangeVisualAt(Vector2 point, float size)
        {
            var pos = WorldUtils.TilePosToWorldPos(new Vector3(point.x, point.y, World.data.tiles.GetHeightAt(point)));
            (var h, float r) = highlightController.highlightProvider.GetAffectedArea(pos);
            return (h, r >= size * 0.71f);
        }

        void TryExpandNode(QuadTree<(HighlightType h, bool done)> node, Vector2 priorityTilePosition)
        {
            if (node.children.HasValue)
                return;

            if (node.value.done)
                return;

            node.InitializeChildren(CalculateRangeVisualAt);
            var children = node.children!.Value;
            PaintNode(node.pos, node.depth, HighlightType.Unassigned);
            foreach (var child in children)
                PaintNode(child.pos, child.depth, child.value.h);

            if (children.All(v => v.value.done))
            {
                TrySimplify(node);
                return;
            }

            int childSize = textureSize_ >> node.depth + 1;
            float worldSize = childSize / (float)pixelsPerUnit;
            float sizeBonus = worldSize * worldSize * 200000;
            float diversityBonus = children.Any(c => children.NW.value.h != c.value.h) ? 100000 : 0;
            foreach (var child in node.children!.Value)
            {
                float priority = (PixelToTilePos(child.pos, childSize) - priorityTilePosition).sqrMagnitude;
                rangeVisualQueue_.Enqueue(child, priority - sizeBonus - diversityBonus);
            }
        }

        void InitMaterial()
        {
            textureSize_ = 1 << levels;
            texture_ = new(textureSize_, textureSize_, TextureFormat, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            PaintNode(Vector2Int.zero, 0, HighlightType.Clear);
            texture_.Apply(false);
            terrainMaterial.SetTexture(HighlightMap, texture_);
            float worldSize = textureSize_ / (float)pixelsPerUnit;
            offset_ = Vector2.one * ((textureSize_ - 1) / (float)pixelsPerUnit / 2);
            terrainMaterial.SetVector(WorldSize, new(worldSize, worldSize));
            terrainMaterial.SetVector(Offset, offset_ + Vector2.one / pixelsPerUnit / 2);
        }

        void PaintNode(Vector2Int pos, int depth, HighlightType highlightType)
        {
            texture_.SetPixel(pos.x, pos.y, new((int)highlightType / 255f, 0, 0), levels - depth);
        }

        static void TrySimplify(QuadTree<(HighlightType h, bool done)> node)
        {
            if (!node.children.HasValue || !node.children.Value.All(c => c.value.done && c.value.h == node.value.h))
                return;
            node.value.done = true;
            node.children = null;

            if (node.parent != null)
                TrySimplify(node.parent);
        }

        void OnDrawGizmosSelected()
        {
            if (rangeVisuals_ != null)
                DrawGizmosRecursive(rangeVisuals_);
        }

        void DrawGizmosRecursive(QuadTree<(HighlightType h, bool done)> node)
        {
            if (node.children is not null)
            {
                foreach (var child in node.children.Value)
                    DrawGizmosRecursive(child);
            }
            else
            {
                int size = textureSize_ >> node.depth;
                Gizmos.color = highlightController.highlightColors[(int)node.value.h];
                Gizmos.DrawWireCube(WorldUtils.TilePosToWorldPos(PixelToTilePos(node.pos, size)), new(size * 0.8f / pixelsPerUnit, 0.1f, size * 0.8f / pixelsPerUnit));
            }
        }
    }
}