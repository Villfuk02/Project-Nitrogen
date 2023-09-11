using BattleSimulation.Attackers;
using BattleSimulation.World;
using Game.Blueprint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Utils;

namespace BattleSimulation.Placement
{
    public class PlacementController : MonoBehaviour
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
            public static short GetValue(Placement.Relation r)
            {
                return (short)((int)r | 0x8000);
            }
        }

        static readonly int Data = Shader.PropertyToID("_Data");
        LayerMask coarseTerrainMask_;
        [Header("References")]
        [SerializeField] TileSelection tileSelection;
        [SerializeField] Material terrainMaterial;
        [Header("Settings")]
        [SerializeField] Color[] relationColors;
        [SerializeField] float rangeVisualsScale;
        [SerializeField] float minRangeVisualsScale;
        [SerializeField] int rangeVisualsStepsPerFrame;
        [SerializeField] float sqrMovementThreshold;
        [SerializeField] int halfMaxQuadCount;
        [Header("Runtime variables")]
        public PlacementState placementState;
        public Placement placing;
        bool emptiedPlacement_;
        readonly Dictionary<Attacker, Placement.Relation> highlightedAttackers_ = new();
        readonly Dictionary<Tile, Placement.Relation> highlightedTiles_ = new();
        QuadTree<(Placement.Relation r, bool done)> rangeVisuals_;
        readonly PriorityQueue<QuadTree<(Placement.Relation r, bool done)>, float> rangeVisualQueue_ = new();
        ComputeBuffer dataBuffer_;
        CompactQuadTreeNode[] dataArray_;
        int nodeCount_;

        void Awake()
        {
            coarseTerrainMask_ = LayerMask.GetMask(LayerNames.COARSE_TERRAIN);
            InitMaterial();
        }
        void Update()
        {
            if (placing == null)
            {
                if (!emptiedPlacement_)
                {
                    UpdateHighlights(Enumerable.Empty<(Placement.Relation, Attacker)>(), highlightedAttackers_, (a, c) => a.SetHighlightColor(c));
                    UpdateHighlights(Enumerable.Empty<(Placement.Relation, Tile)>(), highlightedTiles_, (t, c) => t.SetHighlightColor(c));
                    rangeVisuals_ = null;
                    rangeVisualQueue_.Clear();
                    nodeCount_ = 0;
                    UpdateMaterial();
                    placementState = new();
                    emptiedPlacement_ = true;
                }
                return;
            }

            var prevState = placementState;
            placementState.hoveredTile = tileSelection.hoveredTile;

            emptiedPlacement_ = false;

            if (Input.GetKeyUp(KeyCode.R))
                placementState.rotation++;

            Ray ray = tileSelection.mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100, coarseTerrainMask_))
                placementState.tilePos = WorldUtils.WorldPosToTilePos(hit.point);

            if (prevState.hoveredTile != placementState.hoveredTile || prevState.rotation != placementState.rotation || (prevState.tilePos - placementState.tilePos).sqrMagnitude > sqrMovementThreshold)
            {
                placing.Setup(placementState);
                UpdateHighlights(placing.GetAffectedTiles(), highlightedTiles_, (t, c) => t.SetHighlightColor(c));
                rangeVisuals_ = new(WorldUtils.WORLD_CENTER, rangeVisualsScale, (CalculateRangeVisualAt(WorldUtils.WORLD_CENTER), false), null);
                rangeVisualQueue_.Clear();
                rangeVisualQueue_.Enqueue(rangeVisuals_, 0);
                nodeCount_ = 1;
            }
            UpdateHighlights(placing.GetAffectedAttackers(), highlightedAttackers_, (a, c) => a.SetHighlightColor(c));

            for (int i = 0; i < rangeVisualsStepsPerFrame; i++)
            {
                if (!StepRangeVisuals())
                    break;
            }

            UpdateMaterial();
        }

        public void Deselect()
        {
            if (placing == null)
                return;
            Destroy(placing.gameObject);
        }

        public void Select(Blueprint blueprint)
        {
            placing = Instantiate(blueprint.prefab, transform).GetComponent<Placement>();
            placing.GetComponent<IBlueprinted>().InitBlueprint(blueprint);
        }

        public bool Place()
        {
            placing.Setup(placementState);
            if (!placing.IsValid())
                return false;
            placing.Place();
            placing = null;
            return true;
        }

        void UpdateHighlights<T>(IEnumerable<(Placement.Relation, T)> newColors, Dictionary<T, Placement.Relation> cache, Action<T, Color> update) where T : Component
        {
            var colorsArray = newColors as (Placement.Relation, T)[] ?? newColors.ToArray();
            foreach (var (relation, element) in colorsArray)
            {
                if (cache.TryGetValue(element, out var cachedRelation) && relation == cachedRelation)
                    continue;

                cache[element] = relation;
                update(element, relationColors[(int)relation]);
            }

            var keep = colorsArray.Select(p => p.Item2).ToHashSet();
            foreach (var element in cache.Keys.Where(element => !keep.Contains(element)).ToArray())
            {
                cache.Remove(element);
                if (element != null)
                    update(element, Color.clear);
            }
        }

        public Placement.Relation CalculateRangeVisualAt(Vector2 point)
        {
            var pos = WorldUtils.TilePosToWorldPos(new Vector3(point.x, point.y, World.WorldData.World.data.tiles.GetHeightAt(point) ?? -1000));
            return placing.GetAffectedRegion(pos);
        }

        public bool StepRangeVisuals()
        {
            if (!rangeVisualQueue_.TryDequeue(out var node, out float _))
                return false;

            if (node.children.HasValue)
                return true;

            if (node.scale < minRangeVisualsScale)
            {
                node.value.done = true;
                if (node.parent != null)
                    CheckDone(node.parent);
                return true;
            }

            var childrenValues = node.GetChildrenPositions.Map(p => (CalculateRangeVisualAt(p), false));
            node.SetChildrenValues(childrenValues);
            nodeCount_ += 4;
            int priorityAdjustment = 0;
            if (childrenValues.NW != childrenValues.NE || childrenValues.NW != childrenValues.SW || childrenValues.NW != childrenValues.SE)
                priorityAdjustment -= 100000;
            foreach (var child in node.children!.Value)
            {
                float priority = (child.pos - (Vector2)placementState.tilePos).sqrMagnitude - child.scale * child.scale * 1000;
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
                    first = CompactQuadTreeNode.GetValue(Placement.Relation.Negative)
                };
                dataArray_[0] = n;
            }
            else
            {
                int count = 2;

                void HandleNode(QuadTree<(Placement.Relation r, bool done)> node, int index)
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
                            dataArray_[index / 2].first = CompactQuadTreeNode.GetValue(node.value.r);
                        else
                            dataArray_[index / 2].second = CompactQuadTreeNode.GetValue(node.value.r);
                    }
                }
                HandleNode(rangeVisuals_, 0);
            }

            dataBuffer_.SetData(dataArray_);
            terrainMaterial.SetBuffer(Data, dataBuffer_);
        }

        void CheckDone(QuadTree<(Placement.Relation r, bool done)> node)
        {
            if (!node.children.HasValue || node.children.Value.Any(c => !c.value.done || c.value.r != node.value.r))
                return;
            node.value.done = true;
            node.children = null;
            nodeCount_ -= 4;

            if (node.parent != null)
                CheckDone(node.parent);
        }

        void OnDrawGizmos()
        {
            if (placing == null)
                return;

            bool valid = placing.IsValid();
            Gizmos.color = valid ? Color.green : Color.red;
            Gizmos.DrawSphere(WorldUtils.TilePosToWorldPos(placementState.tilePos), 0.25f);

            if (rangeVisuals_ != null)
                DrawRecursiveGizmos(rangeVisuals_);
        }

        void DrawRecursiveGizmos(QuadTree<(Placement.Relation r, bool done)> node)
        {
            if (node.children is null)
            {
                Gizmos.color = relationColors[(int)node.value.r];
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
