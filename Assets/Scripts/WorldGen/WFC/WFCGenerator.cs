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
        RandomSet<WFCSlot> dirty_;
        WFCState state_;
        FixedCapacityStack<WFCState> stateStack_;
        public static float MaxEntropy { get; private set; }
        int steps_;

        /// <summary>
        /// Generate terrain using the wave function collapse algorithm.
        /// </summary>
        public WFCState Generate(Vector2Int[][] paths)
        {
            // debug
            WaitForStep(StepType.Phase);
            print("Starting WFC");
            // end debug

            InitWFC(paths);

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

        bool TryPropagateConstraints()
        {
            while (dirty_.Count > 0)
            {
                WaitForStep(StepType.MicroStep);
                if (!TryUpdateNext())
                    return false;

                RegisterGizmos(StepType.MicroStep, MakeEntropyGizmos);
            }

            return true;
        }

        void InitWFC(Vector2Int[][] paths)
        {
            int heightCount = TerrainType.MaxHeight + 1;
            MaxEntropy = CalculateEntropy(TerrainType.Modules.GroupBy(m => m.Weight).ToDictionary(g => g.Key, g => g.Count() * heightCount));

            state_ = new();
            dirty_ = new(WorldGenerator.Random.NewSeed());
            foreach (var pos in WorldUtils.WORLD_SIZE + Vector2Int.one)
            {
                WFCSlot s = new(pos, ref state_);
                state_.slots[pos] = s;
                dirty_.Add(s);
            }

            WFCTile centerTile = state_.GetTileAt(WorldUtils.WORLD_CENTER);
            centerTile.slants.Clear();
            centerTile.slants.Add(WorldUtils.Slant.None);

            InitPassages(paths);

            stateStack_ = new(backtrackingDepth);
            steps_ = 0;
        }

        /// <summary>
        /// Assigns which passages can or cannot be passable based on the planned paths.
        /// A passage must exist from a tile on the edge of the world over the edge.
        /// A passage can't exist between two tiles that both have a path going through them, but the paths' distances to center differ by more than one.
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

        void InitTilePassages(int distance, Vector2Int pos, IReadOnlyArray2D<int> pathDistances)
        {
            if (distance == int.MaxValue)
                return;

            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int neighbor = pos + WorldUtils.CARDINAL_DIRS[direction];
                bool hasNeighbor = pathDistances.TryGet(neighbor, out int neighborDistance);
                GetForcedPassages(distance, hasNeighbor, neighborDistance, out bool passable, out bool impassable);
                state_.SetValidPassageAtTile(pos, direction, (passable, impassable));
                if (passable != impassable)
                    RegisterGizmos(StepType.Step, () => MakePassageGizmos(pos, direction, (passable, impassable)));
            }
        }

        /// <summary>
        /// Calculates whether a passage from a tile to its neighbor can be passable or impassable.
        /// </summary>
        static void GetForcedPassages(int distance, bool hasNeighbor, int neighborDistance, out bool passable, out bool impassable)
        {
            passable = true;
            impassable = true;
            if (!hasNeighbor)
            {
                impassable = false;
                return;
            }

            if (neighborDistance == int.MaxValue)
                return;

            if (Mathf.Abs(neighborDistance - distance) == 1)
                impassable = false;
            else
                passable = false;
        }

        bool TryUpdateNext()
        {
            WFCSlot s = dirty_.PopRandom();
            (WFCSlot n, bool backtrack) = s.UpdateValidModules(state_);
            if (backtrack)
                return TryBacktrack();

            if (n is not null)
            {
                MarkNeighborsDirty(n.pos, n.UpdateConstraints(state_));
                state_.slots[n.pos] = n;
            }
            return true;
        }
        bool TryBacktrack()
        {
            dirty_.Clear();
            Vector2Int lastCollapsedSlot = state_.lastCollapsedSlot;
            var lastCollapsedTo = state_.lastCollapsedTo;
            if (stateStack_.Count == 0)
                return false;

            state_ = stateStack_.Pop();
            state_.slots[lastCollapsedSlot].MarkInvalid(lastCollapsedTo);
            MarkDirty(lastCollapsedSlot);
            return true;
        }

        public void MarkNeighborsDirty(Vector2Int pos, IEnumerable<Vector2Int> offsets)
        {
            foreach (var offset in offsets)
                MarkDirty(pos + offset);
        }
        public void MarkDirty(Vector2Int pos)
        {
            if (!state_.slots.TryGet(pos, out var s) || s.Collapsed.module is not null)
                return;
            dirty_.Add(s);
        }

        /// <summary>
        /// Calculates the entropy of the given weighted random set.
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
            foreach ((Vector2Int pos, float weight) in state_.entropyQueue)
            {
                Color c = dirty_.Contains(state_.slots[pos]) ? Color.red : Color.black;
                float entropy = MaxEntropy - weight;
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
