using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using Utils.Random;
using static WorldGen.WorldGenerator;

namespace WorldGen.WFC
{
    public class WFCGenerator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] int backtrackingDepth;
        [Header("Runtime variables")]
        RandomSet<Vector2Int> dirtySlots_;
        WFCState state_;
        FixedCapacityStack<WFCState> stateStack_;
        public static float MaxEntropy { get; private set; }
        int steps_;

        /// <summary>
        /// Generate terrain using the wave function collapse algorithm.
        /// </summary>
        public WFCState Generate(Vector2Int[][] paths, Vector2Int hubPosition)
        {
            // debug
            WaitForStep(StepType.Phase);
            print("Starting WFC");
            // end debug

            InitWFC(paths, hubPosition);

            while (state_.uncollapsed > 0)
            {
                if (!TryStep())
                {
                    print("WFC failed");
                    return null;
                }
            }

            // debug
            RegisterGizmos(StepType.Phase, MakeMeshGizmos);
            print($"WFC Done in {steps_} steps");
            // end debug
            return state_;
        }

        void InitWFC(Vector2Int[][] paths, Vector2Int hubPosition)
        {
            int heightCount = TerrainType.MaxHeight + 1;
            MaxEntropy = CalculateEntropy(TerrainType.Modules.GroupBy(m => m.Weight).ToDictionary(g => g.Key, g => g.Count() * heightCount));

            state_ = new();
            dirtySlots_ = new(0);
            Vector2Int minHeightSlot = new(WorldGenerator.Random.Int(0, WorldUtils.WORLD_SIZE.x + 1), WorldGenerator.Random.Int(0, WorldUtils.WORLD_SIZE.y + 1));
            Vector2Int maxHeightSlot = minHeightSlot;
            //while (maxHeightSlot.ManhattanDistance(minHeightSlot) < 5)
            maxHeightSlot = new(WorldGenerator.Random.Int(0, WorldUtils.WORLD_SIZE.x + 1), WorldGenerator.Random.Int(0, WorldUtils.WORLD_SIZE.y + 1));

            foreach (var pos in WorldUtils.WORLD_SIZE + Vector2Int.one)
            {
                WFCSlot s;
                if (minHeightSlot == pos)
                    s = new(pos, ref state_, 0);
                else if (maxHeightSlot == pos)
                    s = new(pos, ref state_, TerrainType.MaxHeight);
                else
                    s = new(pos, ref state_);
                state_.slots[pos] = s;
                MarkDirty(pos);
            }

            WFCTile hubTile = state_.GetTileAt(hubPosition);
            hubTile.slants = BitSet32.OneBit((int)WorldUtils.Slant.None);

            InitPassages(paths);

            stateStack_ = new(backtrackingDepth);
            steps_ = 0;
        }


        /// <summary>
        /// Sets which passages must be passable.
        /// A passage must exist between two tiles where a path goes through.
        /// A passage must exist from a path tile on the edge of the world over the edge.
        /// </summary>
        void InitPassages(Vector2Int[][] paths)
        {
            var pathDistances = new Array2D<int>(WorldUtils.WORLD_SIZE);
            pathDistances.Fill(int.MaxValue);
            foreach (var path in paths)
                for (int i = 0; i < path.Length; i++)
                    pathDistances[path[i]] = path.Length - i;

            foreach ((var pos, int distance) in pathDistances.IndexedEnumerable)
                InitTilePassages(distance, pos, pathDistances);
        }

        /// <summary>
        /// Set which passages from this specific tile must be passable.
        /// </summary>
        void InitTilePassages(int distance, Vector2Int pos, IReadOnlyArray2D<int> pathDistances)
        {
            if (distance == int.MaxValue)
                return;

            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int neighbor = pos + WorldUtils.CARDINAL_DIRS[direction];
                bool hasNeighbor = pathDistances.TryGet(neighbor, out int neighborDistance);
                bool passable = MustBePassable(distance, hasNeighbor, neighborDistance);
                if (passable)
                {
                    state_.RemoveImpassableEdgeTypesAtTile(pos, direction);
                    // debug
                    // draw edges that must be passable
                    RegisterGizmos(StepType.Step, () => MakePassageGizmos(pos, direction, (true, false)));
                    // end debug
                }
            }
        }

        /// <summary>
        /// Decides whether a passage from a tile to its neighbor must be passable.
        /// </summary>
        static bool MustBePassable(int distance, bool hasNeighbor, int neighborDistance)
        {
            if (!hasNeighbor)
                return true;

            if (neighborDistance == int.MaxValue)
                return false;

            if (Mathf.Abs(neighborDistance - distance) == 1)
                return true;
            return false;
        }

        /// <summary>
        /// Do one step of the WFC algorithm by propagating constraints and then collapsing a random slot.
        /// Fails and returns false if constraint propagation creates an unsolvable state and more backtracking isn't possible.
        /// </summary>
        bool TryStep()
        {
            if (!TryPropagateConstraints())
                return false;

            // debug
            WaitForStep(StepType.Step);
            // end debug

            stateStack_.Push(new(state_));
            state_.CollapseRandom(this);
            steps_++;

            // debug
            RegisterGizmosIfExactly(StepType.Step, MakeEntropyGizmos);
            RegisterGizmos(StepType.Step, MakeMeshGizmos);
            // end debug

            return true;
        }

        /// <summary>
        /// Propagates constraints and backtracks if this propagation creates an unsolvable state.
        /// Fails and returns false when trying to backtrack and more backtracking isn't possible.
        /// </summary>
        /// <returns></returns>
        bool TryPropagateConstraints()
        {
            while (dirtySlots_.Count > 0)
            {
                // debug
                RegisterGizmos(StepType.MicroStep, MakeEntropyGizmos);
                WaitForStep(StepType.MicroStep);
                // end debug

                var slot = dirtySlots_.PopRandom();
                if (TryUpdateSlotConstraints(slot))
                    continue;

                if (!TryBacktrack())
                    return false;
            }

            // debug
            RegisterGizmos(StepType.MicroStep, MakeEntropyGizmos);
            // end debug
            return true;
        }

        /// <summary>
        /// Updates constraints of a given slot.
        /// Returns false if this creates an unsolvable state.
        /// </summary>
        bool TryUpdateSlotConstraints(Vector2Int pos)
        {
            WFCSlot s = state_.slots[pos];
            (WFCSlot n, bool backtrack) = s.UpdateValidModules(state_);
            if (backtrack)
                return false;

            if (n is not null)
            {
                MarkNeighborsDirty(pos, n.UpdateConstraints(state_));
                state_.slots[pos] = n;
                state_.changedSlots[pos] = true;
            }

            return true;
        }

        /// <summary>
        /// Backtracks to the previous state, but removes the option to collapse to the state that was just collapsed.
        /// Fails and returns false more backtracking isn't possible.
        /// </summary>
        bool TryBacktrack()
        {
            dirtySlots_.Clear();
            Vector2Int lastCollapsedSlot = state_.lastCollapsedSlot;
            var lastCollapsedTo = state_.lastCollapsedTo;
            if (stateStack_.Count == 0)
                return false;

            state_ = stateStack_.Pop();
            state_.slots[lastCollapsedSlot].MarkInvalid(lastCollapsedTo);
            MarkDirty(lastCollapsedSlot);
            return true;
        }

        /// <summary>
        /// Marks dirty all slots that are at the given offsets relative to the given slot.
        /// </summary>
        public void MarkNeighborsDirty(Vector2Int pos, IEnumerable<Vector2Int> offsets)
        {
            foreach (var offset in offsets)
                MarkDirty(pos + offset);
        }

        /// <summary>
        /// Marks dirty the slot at the given position, meaning that its constraints need to be recalculated.
        /// </summary>
        public void MarkDirty(Vector2Int pos)
        {
            if (!state_.slots.TryGet(pos, out var s) || s.Collapsed.module is not null)
                return;
            dirtySlots_.Add(pos);
        }

        /// <summary>
        /// Calculates the entropy of a weighted random set, calculated from the weights.
        /// </summary>
        /// <param name="weights">Weights with their respective number of occurrences.</param>
        /// <returns></returns>
        public static float CalculateEntropy(Dictionary<float, int> weights)
        {
            float totalWeight = weights.Sum(w => w.Value * w.Key);
            float totalEntropy = 0;
            foreach (var w in weights)
            {
                float probability = w.Key / totalWeight;
                totalEntropy -= probability * w.Value * Mathf.Log(probability, 2);
            }

            return totalEntropy;
        }

        IEnumerable<GizmoManager.GizmoObject> MakeEntropyGizmos()
        {
            var gizmos = new List<GizmoManager.GizmoObject>();
            foreach ((Vector2Int pos, _) in state_.uncollapsedSlots)
            {
                Color c = dirtySlots_.Contains(pos) ? Color.red : Color.black;
                float entropy = state_.slots[pos].CalculateEntropy();
                float size;
                if (entropy <= MaxEntropy * 0.2f)
                {
                    size = entropy / (MaxEntropy * 0.2f);
                    c += Color.green;
                }
                else
                {
                    size = entropy / MaxEntropy;
                    c += Color.blue;
                }

                gizmos.Add(new GizmoManager.Cube(
                    c,
                    WorldUtils.SlotPosToWorldPos(pos.x, pos.y),
                    size * 0.6f
                ));
            }

            return gizmos;
        }

        IEnumerable<GizmoManager.GizmoObject> MakeMeshGizmos()
        {
            return state_.slots.Where(s => s.Collapsed.module is not null)
                .Select(s => new { s, m = s.Collapsed })
                .Select(t => new GizmoManager.Mesh(Color.white, t.m.module.CollisionMesh,
                    WorldUtils.SlotPosToWorldPos(t.s.pos.x, t.s.pos.y, t.m.height + t.m.module.HeightOffset),
                    new(t.m.module.Flipped ? -1 : 1, 1, 1), Quaternion.Euler(0, 90 * t.m.module.Rotated, 0))).ToList();
        }

        static GizmoManager.Cube MakePassageGizmos(Vector2Int tilePos, int direction, (bool passable, bool impassable) p)
        {
            Color c = p switch
            {
                (false, false) => Color.blue,
                (true, false) => Color.green,
                (false, true) => Color.red,
                (true, true) => Color.yellow
            };
            return new(c, WorldUtils.TilePosToWorldPos(tilePos + 0.5f * (Vector2)WorldUtils.CARDINAL_DIRS[direction]), 0.25f);
        }
    }
}