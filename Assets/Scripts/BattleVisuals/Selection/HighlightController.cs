using BattleSimulation.Selection;
using BattleSimulation.World.WorldData;
using BattleVisuals.Selection.Highlightable;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Utils;
using Object = UnityEngine.Object;

namespace BattleVisuals.Selection
{
    public class HighlightController : MonoBehaviour
    {
        // sent to shader
        struct CompactQuadTreeNode
        {
            // ReSharper disable twice NotAccessedField.Local
            public short first;
            public short second;

            public static short GetValue(short childrenIndex)
            {
                return childrenIndex;
            }
            public static short GetValue(IHighlightable.HighlightType h)
            {
                return (short)((int)h | 0x8000);
            }
        }

        static readonly int Data = Shader.PropertyToID("_Data");
        [Header("References")]
        [SerializeField] SelectionController selection;
        [SerializeField] Material terrainMaterial;
        [Header("Settings")]
        [SerializeField] Color[] highlightColors;
        [SerializeField] float rangeVisualsScale;
        [SerializeField] float rangeVisualsMinScale;
        [SerializeField] float areaSamplesPerFrameMultiplier;
        [SerializeField] int halfMaxQuadCount;
        [Header("Runtime variables")]
        [SerializeField] HighlightProvider lastHighlightProvider;
        [SerializeField] bool doFixedReset;
        [SerializeField] bool doReset;
        readonly Dictionary<IHighlightable, IHighlightable.HighlightType> highlighted_ = new();
        QuadTree<(IHighlightable.HighlightType h, bool done)> rangeVisuals_;
        readonly PriorityQueue<QuadTree<(IHighlightable.HighlightType h, bool done)>, float> rangeVisualQueue_ = new();
        ComputeBuffer dataBuffer_;
        CompactQuadTreeNode[] dataArray_;
        int nodeCount_;

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
                nodeCount_ = 0;
                UpdateMaterial();
                lastHighlightProvider = null;
                return;
            }

            if (doReset || hp != lastHighlightProvider || Input.GetKeyDown(KeyCode.R))
            {
                if (doReset && !doFixedReset)
                    doReset = false;

                lastHighlightProvider = hp;
                rangeVisuals_ = new(WorldUtils.WORLD_CENTER, rangeVisualsScale, CalculateRangeVisualAt(WorldUtils.WORLD_CENTER, rangeVisualsScale), null);
                rangeVisualQueue_.Clear();
                rangeVisualQueue_.Enqueue(rangeVisuals_, 0);
                nodeCount_ = 1;
            }
            UpdateHighlights(hp.GetHighlights(), hovered, selection.placing == null || selection.placing.IsValid());

            lastHighlightProvider = hp;
            int steps = Mathf.CeilToInt(hp.AreaSamplesPerFrame * areaSamplesPerFrameMultiplier);
            for (int i = 0; i < steps; i++)
            {
                if (!StepRangeVisuals())
                    break;
            }

            UpdateMaterial();
        }

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

        public (IHighlightable.HighlightType h, bool done) CalculateRangeVisualAt(Vector2 point, float scale)
        {
            var pos = WorldUtils.TilePosToWorldPos(new Vector3(point.x, point.y, World.data.tiles.GetHeightAt(point) ?? -1000));
            (var h, float r) = lastHighlightProvider.GetAffectedArea(pos);
            return (h, scale <= rangeVisualsMinScale || r >= scale);
        }

        public bool StepRangeVisuals()
        {
            if (!rangeVisualQueue_.TryDequeue(out var node, out float _))
                return false;

            if (node.children.HasValue)
                return true;

            if (node.value.done)
                return true;

            float childrenScale = node.scale / 2;
            var childrenValues = node.GetChildrenPositions.Map(p => CalculateRangeVisualAt(p, childrenScale));
            node.SetChildrenValues(childrenValues);
            nodeCount_ += 4;
            if (childrenValues.All(v => v.done))
            {
                CheckDone(node);
                return true;
            }
            int priorityAdjustment = 0;
            if (childrenValues.NW != childrenValues.NE || childrenValues.NW != childrenValues.SW || childrenValues.NW != childrenValues.SE)
                priorityAdjustment -= 100000;
            foreach (var child in node.children!.Value)
            {
                float priority = (child.pos - (Vector2)selection.hoverTilePosition).sqrMagnitude - child.scale * child.scale * 1000;
                rangeVisualQueue_.Enqueue(child, priority + priorityAdjustment);
            }

            return true;
        }

        void InitMaterial()
        {
            dataBuffer_ = new(halfMaxQuadCount, Marshal.SizeOf(typeof(CompactQuadTreeNode)));
            dataArray_ = new CompactQuadTreeNode[halfMaxQuadCount];
            UpdateMaterial();
        }

        void UpdateMaterial()
        {
            if (nodeCount_ > 2 * halfMaxQuadCount - 1)
                Debug.LogWarning($"{nodeCount_} nodes is over the allowed maximum {2 * halfMaxQuadCount - 1}");

            if (nodeCount_ == 0)
            {
                CompactQuadTreeNode n = new()
                {
                    first = CompactQuadTreeNode.GetValue(IHighlightable.HighlightType.Negative)
                };
                dataArray_[0] = n;
            }
            else
            {
                int count = 2;

                void HandleNode(QuadTree<(IHighlightable.HighlightType h, bool done)> node, int index)
                {
                    if (node.children.HasValue && count <= 2 * halfMaxQuadCount - 5)
                    {
                        int cc = count;
                        if (index % 2 == 0)
                            dataArray_[index / 2].first = CompactQuadTreeNode.GetValue((short)count);
                        else
                            dataArray_[index / 2].second = CompactQuadTreeNode.GetValue((short)count);
                        count += 4;
                        HandleNode(node.children.Value.SE, cc);
                        HandleNode(node.children.Value.NE, cc + 1);
                        HandleNode(node.children.Value.SW, cc + 2);
                        HandleNode(node.children.Value.NW, cc + 3);
                    }
                    else
                    {
                        if (index % 2 == 0)
                            dataArray_[index / 2].first = CompactQuadTreeNode.GetValue(node.value.h);
                        else
                            dataArray_[index / 2].second = CompactQuadTreeNode.GetValue(node.value.h);
                    }
                }
                HandleNode(rangeVisuals_, 0);
            }

            dataBuffer_.SetData(dataArray_);
            terrainMaterial.SetBuffer(Data, dataBuffer_);
        }

        void CheckDone(QuadTree<(IHighlightable.HighlightType h, bool done)> node)
        {
            if (!node.children.HasValue || node.children.Value.Any(c => !c.value.done || c.value.h != node.value.h))
                return;
            node.value.done = true;
            node.children = null;
            nodeCount_ -= 4;

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
                Gizmos.DrawWireCube(WorldUtils.TilePosToWorldPos(node.pos), new(node.scale * 2, 0.1f, node.scale * 2));
            }
            else
            {
                foreach (var child in node.children.Value)
                {
                    DrawRecursiveGizmos(child);
                }
            }
        }

        void OnDestroy()
        {
            dataBuffer_.Release();
        }
    }
}
