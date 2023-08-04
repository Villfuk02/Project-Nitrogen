using Data.WorldGen;
using Random;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using static WorldGen.WorldGenerator;

namespace WorldGen.WFC
{
    public class WFCGenerator : MonoBehaviour
    {
        [SerializeField] int backtrackingDepth;

        // Runtime variables
        public static float MaxEntropy { get; private set; }
        RandomSet<WFCSlot> dirty_;
        WFCState state_;
        FixedCapacityStack<WFCState> stateStack_;

        /// <summary>
        /// Generate the terrain using the Wave function collapse algorithm.
        /// </summary>
        public WFCState Generate(Vector2Int[][] paths)
        {
            WaitForStep(StepType.Phase);
            Debug.Log("Starting WFC");

            int heightCount = WorldGenerator.TerrainType.MaxHeight + 1;
            MaxEntropy = CalculateEntropy(WorldGenerator.TerrainType.Modules.GroupBy(m => m.Weight).ToDictionary(g => g.Key, g => g.Count() * heightCount));

            int[] flatPathDistances = new int[WorldUtils.WORLD_SIZE.x * WorldUtils.WORLD_SIZE.y];
            Array.Fill(flatPathDistances, int.MaxValue);
            var pathDistances = new Array2D<int>(flatPathDistances, WorldUtils.WORLD_SIZE);
            foreach (var path in paths)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    pathDistances[path[i]] = path.Length - i;
                }
            }

            InitWFC(pathDistances);

            stateStack_ = new(backtrackingDepth);
            int steps = 0;
            while (state_.uncollapsed > 0)
            {
                while (dirty_.Count > 0)
                {
                    WaitForStep(StepType.MicroStep);
                    UpdateNext();
                    //we've backtracked too many times
                    if (state_ is null)
                    {
                        Debug.Log("WFC failed");
                        return null;
                    }
                    RegisterGizmos(StepType.MicroStep, DrawEntropy);
                }
                if (steps == 0)
                    Debug.Log("Initial position solved");
                WaitForStep(StepType.Step);
                stateStack_.Push(new(state_));
                state_.CollapseRandom(this);
                steps++;
                RegisterGizmosIfExactly(StepType.Step, DrawEntropy);
                RegisterGizmos(StepType.Step, DrawMesh);
            }

            RegisterGizmos(StepType.Phase, DrawMesh);
            Debug.Log($"WFC Done in {steps} steps");
            return state_;
        }
        void InitWFC(IReadOnlyArray2D<int> pathDistances)
        {
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

            foreach ((var pos, int distance) in pathDistances.IndexedEnumerable)
            {
                if (distance == int.MaxValue)
                    continue;

                for (int direction = 0; direction < 4; direction++)
                {
                    Vector2Int neighbor = pos + WorldUtils.CARDINAL_DIRS[direction];
                    //if there is no neighbor, there's the edge of the word in this direction, there must be a passage, because a path could start from there
                    //if the neighbor's distance differs exactly by one, the path probably goes through here, so there must be a passage
                    if (!pathDistances.TryGet(neighbor, out int neighborDistance) || neighborDistance - distance == 1 || neighborDistance - distance == -1)
                    {
                        RegisterGizmos(StepType.Step, () => DrawPassage(pos, direction, (true, false)));
                        state_.SetValidPassageAtTile(pos, direction, (true, false));
                    }
                    //if the neighbor's distance differs more, but a path still goes through it, it's a different path and they must be separated
                    else if (neighborDistance != int.MaxValue)
                    {
                        RegisterGizmos(StepType.Step, () => DrawPassage(pos, direction, (false, true)));
                        state_.SetValidPassageAtTile(pos, direction, (false, true));
                    }
                }
            }
        }

        void UpdateNext()
        {
            WFCSlot s = dirty_.PopRandom();
            (WFCSlot n, bool backtrack) = s.UpdateValidModules(state_);
            if (backtrack)
            {
                Backtrack();
            }
            else if (n is not null)
            {
                MarkNeighborsDirty(n.pos, n.UpdateConstraints(state_));
                state_.slots[n.pos] = n;
            }
        }
        void Backtrack()
        {
            dirty_.Clear();
            Vector2Int lastCollapsedSlot = state_.lastCollapsedSlot;
            (Module module, int height) lastCollapsedTo = state_.lastCollapsedTo;
            //if it can't backtrack anymore or it has backtracked so many times the stateStack is empty, set state to null to signify backtracking is not possible anymore
            if (stateStack_.Count == 0)
            {
                state_ = null;
            }
            else
            {
                state_ = stateStack_.Pop();
                state_.slots[lastCollapsedSlot].MarkInvalid(lastCollapsedTo);
                MarkDirty(lastCollapsedSlot);
            }

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

        IEnumerable<GizmoManager.GizmoObject> DrawEntropy()
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

        IEnumerable<GizmoManager.GizmoObject> DrawMesh()
        {
            return state_.slots.Where(s => s.Collapsed.module is not null)
                .Select(s => new { s, m = s.Collapsed })
                .Select(t => new GizmoManager.Mesh(Color.white, t.m.module.Collision,
                    WorldUtils.SlotPosToWorldPos(t.s.pos.x, t.s.pos.y, t.m.height + t.m.module.HeightOffset),
                    new(t.m.module.Flipped ? -1 : 1, 1, 1), Quaternion.Euler(0, 90 * t.m.module.Rotated, 0))).ToList();
        }

        static GizmoManager.Cube DrawPassage(Vector2Int tilePos, int direction, (bool passable, bool unpassable) p)
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
