using BattleSimulation.Selection;
using BattleSimulation.World.WorldData;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Object = UnityEngine.Object;

namespace BattleVisuals.Selection
{
    public class HighlightController : MonoBehaviour
    {
        static readonly TextureFormat TextureFormat = TextureFormat.R8;
        static readonly int HighlightMap = Shader.PropertyToID("_HighlightMap");
        static readonly int WorldSize = Shader.PropertyToID("_WorldSize");
        static readonly int Offset = Shader.PropertyToID("_Offset");
        [Header("References")]
        [SerializeField] SelectionController selection;
        [SerializeField] Material terrainMaterial;
        [Header("Settings")]
        [SerializeField] Color[] highlightColors;
        [SerializeField] int pixelsPerUnit;
        [SerializeField] int layers;
        [SerializeField] float areaSamplesPerFrameMultiplier;
        [Header("Runtime variables")]
        [SerializeField] HighlightProvider lastHighlightProvider;
        [SerializeField] bool doFixedReset;
        [SerializeField] bool doReset;
        readonly Dictionary<IHighlightable, IHighlightable.HighlightType> highlighted_ = new();
        QuadTree<(IHighlightable.HighlightType h, bool done)> rangeVisuals_;
        readonly PriorityQueue<QuadTree<(IHighlightable.HighlightType h, bool done)>, float> rangeVisualQueue_ = new();
        int textureSize_;
        Vector2 offset_;
        Texture2D texture_;
        byte[] resetArray_;

        void Awake()
        {
            InitMaterial();
        }
        void Update()
        {
            if (selection.resetVisuals)
            {
                selection.resetVisuals = false;
                doFixedReset = true;
                doReset = true;
            }

            HighlightProvider hp = null;
            if (selection.selected != null)
                hp = selection.selected.GetComponent<HighlightProvider>();
            else if (selection.placing != null)
                hp = selection.placing.GetComponent<HighlightProvider>();
            else if (selection.hovered != null)
                hp = selection.hovered.GetComponent<HighlightProvider>();

            IHighlightable hovered = null;
            if (selection.hovered != null)
                hovered = (IHighlightable)selection.hovered.attacker ?? selection.hovered.tile;


            if (hp == null)
            {
                UpdateHighlights(null, hovered);
                doFixedReset = false;
                doReset = false;

                if (lastHighlightProvider == null)
                    return;

                rangeVisuals_ = null;
                rangeVisualQueue_.Clear();
                for (int m = 0; m <= layers; m++)
                    texture_.SetPixelData(resetArray_, m);
                texture_.Apply(false);
                lastHighlightProvider = null;
                return;
            }

            if (doReset || hp != lastHighlightProvider || Input.GetKeyDown(KeyCode.R))
            {
                if (doReset && !doFixedReset)
                    doReset = false;

                lastHighlightProvider = hp;
                rangeVisuals_ = new(Vector2Int.zero, textureSize_, CalculateRangeVisualAt(Vector2.zero, textureSize_), null);
                rangeVisualQueue_.Clear();
                rangeVisualQueue_.Enqueue(rangeVisuals_, 0);
                for (int m = 0; m <= layers; m++)
                    texture_.SetPixelData(resetArray_, m);
                DrawNode(rangeVisuals_.pos, rangeVisuals_.scale, rangeVisuals_.value.h);
                texture_.Apply(false);
            }
            UpdateHighlights(hp.GetHighlights(), hovered, selection.placing == null || selection.placing.IsValid());

            lastHighlightProvider = hp;
            int steps = Mathf.CeilToInt(hp.AreaSamplesPerFrame * areaSamplesPerFrameMultiplier);
            for (int i = 0; i < steps; i++)
            {
                if (!StepRangeVisuals())
                    break;
            }

            texture_.Apply(false);
        }

        Vector2 CenteredPos(Vector2Int pixelPos, int scale) => (pixelPos + (scale - 1) * 0.5f * Vector2.one) / pixelsPerUnit - offset_ + WorldUtils.WORLD_CENTER;

        void FixedUpdate()
        {
            if (doFixedReset)
                doFixedReset = false;
        }

        void UpdateHighlights(IEnumerable<(IHighlightable.HighlightType, IHighlightable)> newHighlights, IHighlightable hover, bool isValid = true)
        {
            List<(IHighlightable.HighlightType highlight, IHighlightable element)> highlightList;
            if (newHighlights == null)
                highlightList = new();
            else
                highlightList = new(newHighlights);
            if (hover != null)
            {
                var hoverHighlight = isValid ? IHighlightable.HighlightType.Hovered : IHighlightable.HighlightType.Negative;
                bool replaced = false;
                for (int i = 0; i < highlightList.Count; i++)
                {
                    if (highlightList[i].element == hover)
                    {
                        highlightList[i] = (hoverHighlight, hover);
                        replaced = true;
                        break;
                    }
                }
                if (!replaced)
                    highlightList.Add((hoverHighlight, hover));
            }

            foreach (var (highlight, element) in highlightList)
            {
                if (highlighted_.TryGetValue(element, out var cachedRelation) && highlight == cachedRelation)
                    continue;

                highlighted_[element] = highlight;
                element.Highlight(highlightColors[(int)highlight]);
            }

            var keep = highlightList.Select(p => p.element).ToHashSet();
            foreach (var element in highlighted_.Keys.Where(element => !keep.Contains(element)).ToArray())
            {
                highlighted_.Remove(element);
                if (element as Object)
                    element.Unhighlight();
            }
        }
        (IHighlightable.HighlightType h, bool done) CalculateRangeVisualAt(Vector2Int pixel, int scale)
        {
            (var h, bool d) = CalculateRangeVisualAt(CenteredPos(pixel, scale), scale / (float)pixelsPerUnit);
            return (h, scale == 1 || d);
        }
        public (IHighlightable.HighlightType h, bool done) CalculateRangeVisualAt(Vector2 point, float scale)
        {
            var pos = WorldUtils.TilePosToWorldPos(new Vector3(point.x, point.y, World.data.tiles.GetHeightAt(point) ?? -1000));
            (var h, float r) = lastHighlightProvider.GetAffectedArea(pos);
            return (h, r >= scale * 0.71f);
        }

        public bool StepRangeVisuals()
        {
            if (!rangeVisualQueue_.TryDequeue(out var node, out float _))
                return false;

            if (node.children.HasValue)
                return true;

            if (node.value.done)
                return true;

            int childrenScale = node.scale / 2;
            var childrenValues = node.GetChildrenPositions.Map(p => CalculateRangeVisualAt(p, childrenScale));
            node.SetChildrenValues(childrenValues);
            foreach (var child in node.children!)
            {
                DrawNode(child.pos, child.scale, child.value.h);
            }
            if (childrenValues.All(v => v.done))
            {
                CheckDone(node);
                return true;
            }
            int priorityAdjustment = 0;
            if (childrenValues.NW != childrenValues.NE || childrenValues.NW != childrenValues.SW || childrenValues.NW != childrenValues.SE)
                priorityAdjustment -= 100000;
            Vector2 priorityPos = selection.hoverTilePosition is not null ? (Vector2)selection.hoverTilePosition : Vector2.zero;
            foreach (var child in node.children!.Value)
            {
                float priority = ((Vector2)child.pos / pixelsPerUnit - offset_ - priorityPos).sqrMagnitude - child.scale * child.scale * 200000f / pixelsPerUnit / pixelsPerUnit;
                rangeVisualQueue_.Enqueue(child, priority + priorityAdjustment);
            }

            return true;
        }

        void InitMaterial()
        {
            textureSize_ = 1 << layers;
            resetArray_ = Enumerable.Repeat((byte)0xFF, textureSize_ * textureSize_).ToArray();
            texture_ = new(textureSize_, textureSize_, TextureFormat, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int m = 0; m <= layers; m++)
                texture_.SetPixelData(resetArray_, m);
            texture_.Apply(false);
            terrainMaterial.SetTexture(HighlightMap, texture_);
            float worldSize = textureSize_ / (float)pixelsPerUnit;
            offset_ = Vector2.one * ((textureSize_ - 1) / (float)pixelsPerUnit / 2);
            terrainMaterial.SetVector(WorldSize, new(worldSize, worldSize));
            terrainMaterial.SetVector(Offset, offset_ + Vector2.one / pixelsPerUnit / 2);
        }

        void DrawNode(Vector2Int pos, int scale, IHighlightable.HighlightType highlightType)
        {
            int layer = 0;
            while (scale > 1 << layer)
                layer++;

            texture_.SetPixel(pos.x / scale, pos.y / scale, new((int)highlightType / 255f, 0, 0), layer);
        }

        void CheckDone(QuadTree<(IHighlightable.HighlightType h, bool done)> node)
        {
            if (!node.children.HasValue || node.children.Value.Any(c => !c.value.done || c.value.h != node.value.h))
                return;
            node.value.done = true;
            node.children = null;

            if (node.parent != null)
                CheckDone(node.parent);
        }

        void OnDrawGizmosSelected()
        {
            if (rangeVisuals_ != null)
                DrawRecursiveGizmos(rangeVisuals_);
        }

        void DrawRecursiveGizmos(QuadTree<(IHighlightable.HighlightType h, bool done)> node)
        {
            if (node.children is null)
            {
                Gizmos.color = highlightColors[(int)node.value.h];
                Gizmos.DrawWireCube(WorldUtils.TilePosToWorldPos(CenteredPos(node.pos, node.scale)), new(node.scale * 0.8f / pixelsPerUnit, 0.1f, node.scale * 0.8f / pixelsPerUnit));
            }
            else
            {
                foreach (var child in node.children.Value)
                {
                    DrawRecursiveGizmos(child);
                }
            }
        }
    }
}
