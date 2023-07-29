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
        [SerializeField] int backupDepth;

        // Runtime variables
        public static float MaxEntropy { get; private set; }
        RandomSet<WFCSlot> dirty_;
        WFCState state_;
        FixedCapacityStack<WFCState> stateStack_;

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

            stateStack_ = new(backupDepth);
            int steps = 0;
            while (state_.uncollapsed > 0)
            {
                while (dirty_.Count > 0)
                {
                    WaitForStep(StepType.MicroStep);
                    UpdateNext();
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
            for (int x = 0; x < WorldUtils.WORLD_SIZE.x + 1; x++)
            {
                for (int y = 0; y < WorldUtils.WORLD_SIZE.y + 1; y++)
                {
                    WFCSlot s = new(x, y, ref state_);
                    state_.slots[x, y] = s;
                    dirty_.Add(s);
                }
            }

            WFCTile centerTile = state_.GetTileAt(WorldUtils.ORIGIN);
            centerTile.slants.Clear();
            centerTile.slants.Add(WorldUtils.Slant.None);

            foreach ((var pos, int distance) in pathDistances.IndexedEnumerable)
            {
                if (distance == int.MaxValue)
                    continue;

                for (int i = 0; i < 4; i++)
                {
                    Vector2Int neighbor = pos + WorldUtils.CARDINAL_DIRS[i];
                    if (!pathDistances.TryGet(neighbor, out var neighborDistance) || neighborDistance - distance == 1 || neighborDistance - distance == -1)
                    {
                        RegisterGizmos(StepType.Step, () => DrawPassage(pos, i, (true, false)));
                        state_.SetValidPassageAtTile(pos, i, (true, false));
                    }
                    else if (neighborDistance != int.MaxValue)
                    {
                        RegisterGizmos(StepType.Step, () => DrawPassage(pos, i, (false, true)));
                        state_.SetValidPassageAtTile(pos, i, (false, true));
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
            //Debug.Log("Backtracking");
            dirty_.Clear();
            Vector2Int lastCollapsedSlot = state_.lastCollapsedSlot;
            (Module module, int height) lastCollapsedTo = state_.lastCollapsedTo;
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
            if (!state_.slots.TryGet(pos, out var s) || s.Collapsed is not null)
                return;
            dirty_.TryAdd(s);
        }


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
            foreach ((Vector2Int pos, float weight) in state_.entropyQueue.AllEntries)
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
                    WorldUtils.SlotToWorldPos(pos.x, pos.y),
                    size * 0.6f
                    ));
            }
            return gizmos;
        }

        IEnumerable<GizmoManager.GizmoObject> DrawMesh()
        {
            return state_.slots.Where(s => s.Collapsed is not null)
                .Select(s => new { s, m = s.Collapsed })
                .Select(t => new GizmoManager.Mesh(Color.white, t.m.Collision,
                    WorldUtils.SlotToWorldPos(t.s.pos.x, t.s.pos.y, t.s.Height + t.m.HeightOffset),
                    new(t.m.Flipped ? -1 : 1, 1, 1), Quaternion.Euler(0, 90 * t.m.Rotated, 0))).ToList();
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
            return new(c, WorldUtils.TileToWorldPos(tilePos + 0.5f * (Vector2)WorldUtils.CARDINAL_DIRS[direction]), 0.25f);
        }
    }
}
