using Data.WorldGen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Utils;
using static WorldGen.WorldGenerator;

namespace WorldGen.Decorations
{
    public class ObstacleModelScatterer : MonoBehaviour
    {
        Decoration[] decorations_;
        List<Vector2Int> allTiles_;
        Array2D<List<Vector3>> colliders_;

        /// <summary>
        /// Scatters models that represent obstacles on the tiles that should have obstacles.
        /// </summary>
        public void Scatter()
        {
            // debug
            WaitForStep(StepType.Phase);
            print("Scattering");
            // end debug

            Initialize();

            foreach (var d in decorations_)
            {
                // debug
                WaitForStep(StepType.Step);
                // draw the colliders of already scattered models
                RegisterGizmos(StepType.Step, () => colliders_.SelectMany(list => list, (_, v) => new GizmoManager.Sphere(Color.green, WorldUtils.TilePosToWorldPos(new Vector3(v.x, v.y)), v.z)));
                // end debug

                ScatterStep(d);
            }
            // debug
            print("Scattered");
            // draw the colliders of all scattered models
            RegisterGizmos(StepType.Phase, () => colliders_.SelectMany(list => list, (_, v) => new GizmoManager.Sphere(Color.yellow, WorldUtils.TilePosToWorldPos(new Vector3(v.x, v.y)), v.z)));
            // end debug
        }

        /// <summary>
        /// Prepare fields and initialize noise nodes.
        /// </summary>
        void Initialize()
        {
            decorations_ = WorldGenerator.TerrainType.ScattererData.decorations;
            WorldGenerator.TerrainType.ScattererData.isPath = pos => WorldUtils.IsInRange(pos, WorldUtils.WORLD_SIZE) && Tiles[pos].dist != int.MaxValue;
            var obstacles = WorldGenerator.TerrainType.ScattererData.isObstacle.Keys.Select(n => WorldGenerator.TerrainType.Obstacles.Obstacles[n]).ToArray();
            foreach (var obstacle in obstacles)
            {
                WorldGenerator.TerrainType.ScattererData.isObstacle[obstacle.Name] = pos => WorldUtils.IsInRange(pos, WorldUtils.WORLD_SIZE) && Tiles[pos].obstacle == obstacle;
            }

            foreach (var fractalNoiseNode in WorldGenerator.TerrainType.NoiseNodes)
            {
                fractalNoiseNode.Noise.Init(WorldGenerator.Random.NewSeed());
            }

            allTiles_ = new();
            foreach (Vector2Int v in WorldUtils.WORLD_SIZE)
                allTiles_.Add(v);
            colliders_ = new(WorldUtils.WORLD_SIZE);
            colliders_.Fill(() => new());
        }

        /// <summary>
        /// Scatter one type of decoration.
        /// </summary>
        void ScatterStep(Decoration decoration)
        {
            print($"Scattering {decoration.Name}");

            Array2D<List<Vector3>> currentColliders = new(WorldUtils.WORLD_SIZE);
            Array2D<List<Vector3>> futureColliders = new(WorldUtils.WORLD_SIZE);
            List<Vector2Int> groups = new(9);
            foreach (var group in Vector2Int.one * 3)
                groups.Add(group);
            WorldGenerator.Random.Shuffle(groups);
            WorldGenerator.Random.Shuffle(allTiles_);
            foreach (var group in groups)
            {
                List<Task> tasks = new();
                foreach (var tile in allTiles_)
                {
                    if (tile.x % 3 != group.x || tile.y % 3 != group.y)
                        continue;
                    var seed = WorldGenerator.Random.NewSeed();
                    tasks.Add(Task.Run(() => ScatterTile(decoration, tile, currentColliders, futureColliders, seed)));
                }
                Task.WaitAll(tasks.ToArray());
            }

            foreach (var (tile, list) in futureColliders.IndexedEnumerable)
            {
                colliders_[tile].AddRange(list);
            }
        }

        /// <summary>
        /// Scatter one type of decoration on a given tile, taking into account the colliders already there.
        /// Fills in the colliders of the generated decorations into currentColliders and futureColliders.
        /// </summary>
        void ScatterTile(Decoration decoration, Vector2Int tile, Array2D<List<Vector3>> currentColliders, Array2D<List<Vector3>> futureColliders, ulong randomSeed)
        {
            // debug
            WaitForStep(StepType.MicroStep);
            // draw the current tile and adjacent tiles
            RegisterGizmos(StepType.MicroStep, () => new GizmoManager.Cube(Color.red, WorldUtils.TilePosToWorldPos(tile), new Vector3(2, 0.1f, 2)), tile);
            // end debug

            var relevantColliders = MergeColliderLists(tile, currentColliders);
            List<Vector3> generatedCurrent = new();
            List<Vector3> generatedFuture = new();

            Utils.Random.Random rand = new(randomSeed);

            for (int i = 0; i < decoration.TriesPerTile; i++)
            {
                TryPosition(tile + new Vector2(rand.Float(-0.5f, 0.5f), rand.Float(-0.5f, 0.5f)));
            }

            // debug
            WaitForStep(StepType.MicroStep);
            // draw the generated colliders, both current and future
            RegisterGizmos(StepType.Step, () =>
            {
                List<GizmoManager.GizmoObject> gizmos = new();
                gizmos.AddRange(generatedFuture.Select(v => new GizmoManager.Sphere(Color.green, WorldUtils.TilePosToWorldPos(new Vector3(v.x, v.y)), v.z)));
                gizmos.AddRange(generatedCurrent.Select(v => new GizmoManager.Sphere(Color.cyan, WorldUtils.TilePosToWorldPos(new Vector3(v.x, v.y)), v.z)));
                return gizmos;
            });
            // end debug

            futureColliders[tile] ??= new();
            futureColliders[tile].AddRange(generatedFuture);
            currentColliders[tile] = generatedCurrent;

            ExpireGizmos(tile);
            return;

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

                Vector2 r = rand.InsideUnitCircle() * decoration.AngleSpread;
                Vector3 rotation = new(r.x, rand.Float(0, 360), r.y);

                Tiles[tile].decorations.Add(new() { decoration = decoration, position = pos, size = decoration.GetScale(v), eulerRotation = rotation });
            }
        }
        /// <summary>
        /// Merges the colliders of decorations from the given tile and adjacent tiles into one list.
        /// </summary>
        Vector3[] MergeColliderLists(Vector2Int tile, Array2D<List<Vector3>> currentColliders)
        {
            Dictionary<Vector2, Vector3> merging = new();
            foreach (Vector2Int direction in WorldUtils.ADJACENT_AND_ZERO)
            {
                Vector2Int t = tile + direction;
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
            return merging.Values.ToArray();
        }
    }
}