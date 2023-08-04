using Data.WorldGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utils;
using static WorldGen.WorldGenerator;

namespace WorldGen.Decorations
{
    public class Scatterer : MonoBehaviour
    {
        Decoration[] decorations_;
        List<Vector2Int> allTiles_;
        Array2D<List<Vector3>> colliders_;

        public void Scatter()
        {
            WaitForStep(StepType.Phase);
            Debug.Log("Scattering");

            decorations_ = WorldGenerator.TerrainType.ScattererData.decorations;
            WorldGenerator.TerrainType.ScattererData.isPath = pos => WorldUtils.IsInRange(pos, WorldUtils.WORLD_SIZE) && Tiles[pos].dist != int.MaxValue;
            var blockers = WorldGenerator.TerrainType.ScattererData.isBlocker.Keys.Select(n => WorldGenerator.TerrainType.Blockers.Blockers[n]).ToArray();
            foreach (var blocker in blockers)
            {
                WorldGenerator.TerrainType.ScattererData.isBlocker[blocker.Name] = pos => WorldUtils.IsInRange(pos, WorldUtils.WORLD_SIZE) && Tiles[pos].blocker == blocker;
            }
            foreach (var fractalNoiseNode in FractalNoiseNode.ALL_NODES)
            {
                fractalNoiseNode.Noise.Init(WorldGenerator.Random.NewSeed());
            }

            allTiles_ = new();
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                allTiles_.Add(v);
            colliders_ = new(WorldUtils.WORLD_SIZE);
            foreach (var (pos, _) in colliders_.IndexedEnumerable)
            {
                colliders_[pos] = new();
            }

            foreach (var d in decorations_)
            {
                WaitForStep(StepType.Step);
                RegisterGizmos(StepType.Step, () => colliders_.SelectMany(list => list, (_, v) => new GizmoManager.Sphere(Color.green, WorldUtils.TilePosToWorldPos(new Vector3(v.x, v.y)), v.z)));
                ScatterStep(d);
            }
            Debug.Log("Scattered");
            RegisterGizmos(StepType.Phase, () => colliders_.SelectMany(list => list, (_, v) => new GizmoManager.Sphere(Color.yellow, WorldUtils.TilePosToWorldPos(new Vector3(v.x, v.y)), v.z)));
        }


        void ScatterStep(Decoration decoration)
        {
            Debug.Log($"Scattering {decoration.Name}");

            Array2D<List<Vector3>> currentColliders = new(WorldUtils.WORLD_SIZE);
            Array2D<List<Vector3>> futureColliders = new(WorldUtils.WORLD_SIZE);
            var flatTasks = new Task[allTiles_.Count];
            Array2D<Task> tasks = new(flatTasks, WorldUtils.WORLD_SIZE);
            Array2D<object> locks = new(WorldUtils.WORLD_SIZE);
            foreach (var pos in WorldUtils.WORLD_SIZE)
            {
                locks[pos] = new();
            }
            WorldGenerator.Random.Shuffle(allTiles_);
            foreach (var tile in allTiles_)
            {
                tasks[tile] = Task.Run(() => ScatterTile(decoration, tile, currentColliders, futureColliders, locks, WorldGenerator.Random.NewSeed()));
            }

            Task.WaitAll(flatTasks);

            foreach (var (tile, list) in futureColliders.IndexedEnumerable)
            {
                colliders_[tile].AddRange(list);
            }
        }

        void ScatterTile(Decoration decoration, Vector2Int tile, Array2D<List<Vector3>> currentColliders, Array2D<List<Vector3>> futureColliders, IReadOnlyArray2D<object> locks, ulong randomSeed)
        {
            WaitForStep(StepType.MicroStep);
            RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.yellow, WorldUtils.TilePosToWorldPos(tile), new Vector3(0.7f, 0.1f, 0.7f)), locks[tile]);

            foreach (var offset in new Vector2Int(3, 3))
            {
                if (locks.TryGet(tile + offset - Vector2Int.one, out object l))
                    Monitor.Enter(l);
            }

            try
            {
                ExpireGizmos(locks[tile]);
                RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.red, WorldUtils.TilePosToWorldPos(tile), new Vector3(2, 0.1f, 2)), locks[tile]);

                Dictionary<Vector2, Vector3> merging = new();
                foreach (Vector2Int d in WorldUtils.ADJACENT_AND_ZERO)
                {
                    Vector2Int t = tile + d;
                    if (currentColliders.TryGet(t, out var cList) && cList is not null)
                    {
                        foreach (Vector3 col in cList)
                        {
                            merging.Add(new(col.x, col.y), col);
                        }
                    }

                    if (colliders_.TryGet(t, out var pList) && pList is not null)
                    {
                        foreach (Vector3 col in pList)
                        {
                            merging.TryAdd(new(col.x, col.y), col);
                        }
                    }
                }

                IEnumerable<Vector3> relevantColliders = merging.Values;
                List<Vector3> generatedCurrent = new();
                List<Vector3> generatedFuture = new();

                Random.Random rand = new(randomSeed);

                for (int i = 0; i < decoration.TriesPerTile; i++)
                {
                    Vector2 pos = tile + new Vector2(rand.Float(), rand.Float()) - Vector2.one * 0.5f;
                    TryPosition(pos);
                }

                void TryPosition(Vector2 pos)
                {
                    float v = new DecorationEvaluator(pos).Evaluate(decoration);
                    if (v <= decoration.ValueThreshold)
                        return;

                    float placementRadius = decoration.GetPlacementSize(v);
                    if (relevantColliders.Any(c => c.z > 0 && Vector2.Distance(pos, new(c.x, c.y)) < c.z + placementRadius))
                        return;

                    if (generatedCurrent.Any(c => Vector2.Distance(pos, new(c.x, c.y)) < c.z + placementRadius))
                        return;

                    if (placementRadius > 0)
                        generatedCurrent.Add(new(pos.x, pos.y, placementRadius));
                    float persistentRadius = decoration.GetColliderSize(v);
                    if (persistentRadius > 0)
                        generatedFuture.Add(new(pos.x, pos.y, persistentRadius));
                    Tiles[tile].decorations.Add(new() { decoration = decoration, position = pos, size = decoration.GetScale(v) });
                }

                WaitForStep(StepType.MicroStep);
                RegisterGizmos(StepType.Step, () =>
                {
                    List<GizmoManager.GizmoObject> gizmos = new();
                    gizmos.AddRange(generatedFuture.Select(v => new GizmoManager.Sphere(Color.green, WorldUtils.TilePosToWorldPos(new Vector3(v.x, v.y)), v.z)));
                    gizmos.AddRange(generatedCurrent.Select(v => new GizmoManager.Sphere(Color.cyan, WorldUtils.TilePosToWorldPos(new Vector3(v.x, v.y)), v.z)));
                    return gizmos;
                });

                futureColliders[tile] ??= new();
                futureColliders[tile].AddRange(generatedFuture);
                currentColliders[tile] = generatedCurrent;
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            finally
            {
                foreach (var offset in new Vector2Int(3, 3))
                {
                    if (locks.TryGet(tile - offset + Vector2Int.one, out object l))
                        Monitor.Exit(l);
                }
            }

            ExpireGizmos(locks[tile]);
        }
    }
}