
using Data.WorldGen;
using Random;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;
using static WorldGen.WorldGenerator;

namespace WorldGen.Blockers
{
    public class BlockerGenerator : MonoBehaviour
    {
        RandomSet<Vector2Int> tilesLeft_;
        Array2D<float>[] weightFields_;
        List<Vector2Int> emptyTiles_;
        public void PlaceBlockers(Vector2Int[] pathStarts, int[] pathLengths)
        {
            WaitForStep(StepType.Phase);
            Debug.Log("Picking Blockers");

            RegisterGizmos(StepType.Phase, () => new List<Vector2Int>(pathStarts) { WorldUtils.WORLD_CENTER }.Select(p => new GizmoManager.Cube(Color.yellow, WorldUtils.TilePosToWorldPos(p), 0.5f)));

            var layers = WorldGenerator.TerrainType.Blockers.Layers;
            var fillers = WorldGenerator.TerrainType.Blockers.Fillers;
            weightFields_ = new Array2D<float>[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                weightFields_[i] = new(WorldUtils.WORLD_SIZE);
            }

            emptyTiles_ = new();
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
            {
                if (Tiles[v].dist == int.MaxValue)
                    emptyTiles_.Add(v);
            }
            //first generate blockers on random tiles that don't have a planned path going through them
            int layer = 0;
            List<(BlockerData blocker, int placed)> currentLayer = null;
            tilesLeft_ = new(emptyTiles_, WorldGenerator.Random.NewSeed());
            while (layer < layers.Length)
            {
                currentLayer ??= layers[layer].Select(b => (b, 0)).ToList();
                if (currentLayer.Count == 0)
                {
                    tilesLeft_ = new(emptyTiles_, WorldGenerator.Random.NewSeed());
                    layer++;
                    currentLayer = null;
                    DrawGizmos(StepType.Step);
                    WaitForStep(StepType.Step);
                }
                else
                {
                    if (tilesLeft_.Count == 0)
                    {
                        if (currentLayer.All(p => p.blocker.Min <= p.placed))
                        {
                            layer++;
                            currentLayer = null;
                        }
                        tilesLeft_ = new(emptyTiles_, WorldGenerator.Random.NewSeed());
                        DrawGizmos(StepType.Step);
                        WaitForStep(StepType.Step);
                    }
                    else
                    {
                        DrawGizmos(StepType.MicroStep);
                        WaitForStep(StepType.MicroStep);
                        Vector2Int p = tilesLeft_.PopRandom();
                        TryPlace(p, currentLayer, false);
                    }
                }
            }
            DrawGizmos(StepType.Step);
            WaitForStep(StepType.Step);
            //then fill all tiles with the filler blocker and remove them in a random order, keeping only those that would allow for a shorter path than intended from any start
            currentLayer = fillers.Select(b => (b, 0)).ToList();
            foreach (var pos in tilesLeft_)
            {
                Tiles[pos].passable = false;
            }
            while (tilesLeft_.Count > 0)
            {
                DrawGizmos(StepType.MicroStep);
                WaitForStep(StepType.MicroStep);
                Vector2Int pos = tilesLeft_.PopRandom();
                Tiles[pos].passable = true;
                Tiles.RecalculateDistances();
                bool valid = !pathStarts.Where((s, i) => Tiles[s].dist != pathLengths[i]).Any();
                if (!valid)
                    TryPlace(pos, currentLayer, true);
            }
            Tiles.RecalculateDistances();
            DrawGizmos(StepType.Phase);
            Debug.Log("Blockers Picked");
        }

        void TryPlace(Vector2Int pos, List<(BlockerData blocker, int placed)> available, bool force)
        {
            WeightedRandomSet<int> placed = new(WorldGenerator.Random.NewSeed());
            for (int i = 0; i < available.Count; i++)
            {
                (var b, int p) = available[i];
                float probability = b.BaseProbability;
                bool valid = true;
                if (p >= b.Max || (!b.OnSlants && Tiles[pos].slant != WorldUtils.Slant.None))
                {
                    probability = 0;
                    valid = false;
                }
                else
                {
                    for (int j = 0; j < b.Forces.Length; j++)
                    {
                        if (b.Forces[j] != 0)
                            probability += b.Forces[j] * weightFields_[j][pos];
                    }
                }
                probability = Mathf.Clamp01(probability);
                if (valid && (force || WorldGenerator.Random.Float() < probability))
                    placed.Add(i, probability);
            }

            if (placed.Count == 0)
                return;

            int index = placed.PopRandom();
            var entry = available[index];
            entry.placed++;
            if (entry.placed >= entry.blocker.Max)
                available.RemoveAt(index);
            else
                available[index] = entry;
            Place(pos, entry.blocker, -1);
        }
        void Place(Vector2Int pos, BlockerData blocker, int layer)
        {
            emptyTiles_.Remove(pos);
            Tiles[pos].passable = false;
            Tiles[pos].blocker = blocker;
            if (layer == -1)
                return;
            foreach (var p in emptyTiles_)
            {
                Vector2Int dist = p - pos;
                weightFields_[layer][p] += 1f / dist.sqrMagnitude;
            }
        }

        void DrawGizmos(StepType duration)
        {
            RegisterGizmos(duration, () => tilesLeft_.Select(pos => new GizmoManager.Cube(Color.cyan, WorldUtils.TilePosToWorldPos(pos), 0.3f)));
            RegisterGizmos(duration, () =>
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                foreach (var tile in Tiles)
                {
                    Vector3 pos = WorldUtils.TilePosToWorldPos(tile.pos);
                    Color c = !tile.passable ? Color.magenta : (tile.dist == int.MaxValue ? Color.red : Color.green);
                    gizmos.Add(new GizmoManager.Cube(c, pos, 0.2f));
                    gizmos.AddRange(
                        tile.neighbors.Where(n => n is not null).Select(n => WorldUtils.TilePosToWorldPos(n.pos))
                            .Select(other => new GizmoManager.Line(c, pos, Vector3.Lerp(pos, other, 0.5f)))
                    );
                }
                return gizmos;
            });
        }
    }
}
